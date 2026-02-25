using Microsoft.AspNetCore.Mvc;
using SemanticPoker.Api.Infrastructure.Persistence;
using SemanticPoker.Api.Services;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly IMatchRepository _repository;
    private readonly MatchExecutionQueue _queue;
    private readonly ILogger<MatchesController> _logger;

    public MatchesController(
        IMatchRepository repository,
        MatchExecutionQueue queue,
        ILogger<MatchesController> logger)
    {
        _repository = repository;
        _queue = queue;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MatchListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] MatchStatus? status, CancellationToken ct)
    {
        var matches = await _repository.GetAllAsync(status, ct);
        var response = new MatchListResponse
        {
            Matches = matches.Select(MapToResponse).ToList()
        };
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMatchRequest request, CancellationToken ct)
    {
        if (request.ModelIds.Count < 3)
            return BadRequest(new ErrorResponse { Error = "At least 3 models are required." });

        var match = new Match
        {
            Config = new MatchConfig
            {
                TotalRounds = request.TotalRounds,
                ModelIds = request.ModelIds,
                RotateArchitect = request.RotateArchitect,
                AdaptivePlay = request.AdaptivePlay,
                RandomSeed = request.RandomSeed,
                OllamaBaseUrl = request.OllamaBaseUrl ?? "http://localhost:11434",
                LlmTimeoutSeconds = request.LlmTimeoutSeconds ?? 120,
                LlmTemperature = request.LlmTemperature ?? 0.7
            }
        };

        await _repository.CreateAsync(match, ct);
        await _queue.EnqueueAsync(match.Id, ct);

        _logger.LogInformation("Match {MatchId} created and queued with models: {Models}",
            match.Id, string.Join(", ", request.ModelIds));

        return AcceptedAtAction(nameof(GetById), new { id = match.Id }, MapToResponse(match));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match is null) return NotFound();
        return Ok(MapToResponse(match));
    }

    [HttpGet("{id:guid}/rounds")]
    [ProducesResponseType(typeof(List<RoundResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRounds(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match is null) return NotFound();

        var rounds = match.Rounds
            .OrderBy(r => r.RoundNumber)
            .Select(MapToRoundResponse)
            .ToList();

        return Ok(rounds);
    }

    [HttpGet("{id:guid}/rounds/{roundNumber:int}")]
    [ProducesResponseType(typeof(RoundDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoundDetail(Guid id, int roundNumber, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match is null) return NotFound();

        var round = match.Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round is null) return NotFound();

        var detail = new RoundDetailResponse
        {
            RoundNumber = round.RoundNumber,
            Phase = round.Phase,
            ArchitectModelId = round.ArchitectModelId,
            TreasureDoor = round.GameState.TreasureDoor.Label,
            TrapDoor = round.GameState.TrapDoor.Label,
            ShuffledSentences = round.ShuffledSentences.Select((s, i) => new SentenceDto
            {
                Index = i,
                Text = s.Text,
                Source = s.Source,
                IsTruthful = s.IsTruthful
            }).ToList(),
            PlayerDecisions = round.PlayerDecisions
                .Where(d => d.Role == PlayerRole.Player)
                .Select(d => new PlayerDecisionDto
                {
                    ModelId = d.ModelId,
                    Role = d.Role,
                    ChosenDoor = d.ChosenDoor,
                    DoorOutcome = d.DoorOutcome,
                    ScoreChange = d.ScoreChange,
                    Reasoning = d.Reasoning,
                    ResponseTimeMs = d.ResponseTimeMs
                }).ToList(),
            PlayerChoices = round.PlayerDecisions
                .Where(d => d.Role == PlayerRole.Player)
                .ToDictionary(d => d.ModelId, d => d.ChosenDoor),
            ScoreChanges = round.Result?.PlayerScoreChanges ?? new(),
            StartedAt = round.StartedAt,
            CompletedAt = round.CompletedAt
        };

        return Ok(detail);
    }

    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match is null) return NotFound();

        if (match.Status != MatchStatus.Running)
            return BadRequest(new ErrorResponse { Error = $"Cannot pause a match with status '{match.Status}'." });

        match.Status = MatchStatus.Paused;
        await _repository.UpdateAsync(match, ct);

        _logger.LogInformation("Match {MatchId} paused", id);
        return Ok(MapToResponse(match));
    }

    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match is null) return NotFound();

        if (match.Status != MatchStatus.Paused)
            return BadRequest(new ErrorResponse { Error = $"Cannot resume a match with status '{match.Status}'." });

        match.Status = MatchStatus.Running;
        await _repository.UpdateAsync(match, ct);
        await _queue.EnqueueAsync(match.Id, ct);

        _logger.LogInformation("Match {MatchId} resumed", id);
        return AcceptedAtAction(nameof(GetById), new { id }, MapToResponse(match));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match is null) return NotFound();

        if (match.Status is MatchStatus.Completed or MatchStatus.Failed)
            return BadRequest(new ErrorResponse { Error = $"Cannot cancel a match with status '{match.Status}'." });

        match.Status = MatchStatus.Cancelled;
        match.CompletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(match, ct);

        _logger.LogInformation("Match {MatchId} cancelled", id);
        return Ok(MapToResponse(match));
    }

    private static MatchResponse MapToResponse(Match match)
    {
        return new MatchResponse
        {
            Id = match.Id,
            Status = match.Status,
            TotalRounds = match.Config.TotalRounds,
            CompletedRounds = match.Rounds.Count(r => r.Phase == RoundPhase.Completed),
            ModelIds = match.Config.ModelIds,
            Scores = match.Scores,
            CreatedAt = match.CreatedAt,
            StartedAt = match.StartedAt,
            CompletedAt = match.CompletedAt,
            ErrorMessage = match.ErrorMessage
        };
    }

    private static RoundResponse MapToRoundResponse(Round round)
    {
        return new RoundResponse
        {
            RoundNumber = round.RoundNumber,
            Phase = round.Phase,
            ArchitectModelId = round.ArchitectModelId,
            PlayerChoices = round.PlayerDecisions
                .Where(d => d.Role == PlayerRole.Player)
                .ToDictionary(d => d.ModelId, d => d.ChosenDoor),
            ScoreChanges = round.Result?.PlayerScoreChanges ?? new(),
            StartedAt = round.StartedAt,
            CompletedAt = round.CompletedAt
        };
    }
}
