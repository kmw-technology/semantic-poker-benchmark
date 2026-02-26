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
    private readonly HumanInputCoordinator _humanInput;
    private readonly ILogger<MatchesController> _logger;

    public MatchesController(
        IMatchRepository repository,
        MatchExecutionQueue queue,
        HumanInputCoordinator humanInput,
        ILogger<MatchesController> logger)
    {
        _repository = repository;
        _queue = queue;
        _humanInput = humanInput;
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
                .Select(d => new PlayerDecisionDto
                {
                    ModelId = d.ModelId,
                    Role = d.Role,
                    ChosenDoor = d.ChosenDoor,
                    DoorOutcome = d.DoorOutcome,
                    ScoreChange = d.ScoreChange,
                    Reasoning = d.Reasoning,
                    ResponseTimeMs = d.ResponseTimeMs,
                    RawResponse = d.RawResponse,
                    PromptTokens = d.PromptTokens,
                    CompletionTokens = d.CompletionTokens,
                    SystemPrompt = d.SystemPrompt,
                    UserPrompt = d.UserPrompt,
                    ParseStrategy = d.ParseStrategy,
                    ParseSuccess = d.ParseSuccess,
                    FinishReason = d.FinishReason
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

        // Unblock the game loop if waiting for human input
        _humanInput.CancelMatch(id);

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
            ErrorMessage = match.ErrorMessage,
            IsInteractive = match.Config.IsInteractive,
            HumanPlayerNames = match.Config.HumanPlayerNames
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

    [HttpPost("interactive")]
    public async Task<IActionResult> CreateInteractive(
        [FromBody] CreateInteractiveMatchRequest request, CancellationToken ct)
    {
        if (request.Players == null || request.Players.Count < 3)
            return BadRequest(new { Error = "At least 3 players are required." });

        var llmPlayers = request.Players.Where(p => p.Type == "LLM").ToList();
        var humanPlayers = request.Players.Where(p => p.Type == "Human").ToList();

        if (llmPlayers.Count == 0)
            return BadRequest(new { Error = "At least 1 LLM player is required." });

        if (llmPlayers.Any(p => string.IsNullOrWhiteSpace(p.ModelId)))
            return BadRequest(new { Error = "All LLM players must have a model selected." });

        if (humanPlayers.Any(p => string.IsNullOrWhiteSpace(p.Name)))
            return BadRequest(new { Error = "All human players must have a name." });

        var humanNames = humanPlayers.Select(p => p.Name!.Trim()).ToList();
        if (humanNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != humanNames.Count)
            return BadRequest(new { Error = "Human player names must be unique." });

        // Build ModelIds list: human slots → "human:{name}", LLM slots → modelId
        // Deduplicate: if the same model appears multiple times, append #2, #3, etc.
        var idCounts = new Dictionary<string, int>();
        var allModelIds = new List<string>();
        foreach (var p in request.Players)
        {
            var rawId = p.Type == "Human" ? $"human:{p.Name!.Trim()}" : p.ModelId!;
            idCounts.TryGetValue(rawId, out var count);
            idCounts[rawId] = count + 1;
            allModelIds.Add(count == 0 ? rawId : $"{rawId}#{count + 1}");
        }

        var match = new Match
        {
            Id = Guid.NewGuid(),
            Status = MatchStatus.Pending,
            Config = new MatchConfig
            {
                TotalRounds = request.TotalRounds,
                ModelIds = allModelIds,
                RotateArchitect = request.RotateArchitect,
                AdaptivePlay = request.AdaptivePlay,
                RandomSeed = request.RandomSeed,
                OllamaBaseUrl = request.OllamaBaseUrl ?? "http://localhost:11434",
                LlmTimeoutSeconds = request.LlmTimeoutSeconds ?? 120,
                LlmTemperature = request.LlmTemperature ?? 0.7,
                IsInteractive = true,
                HumanPlayerNames = humanNames
            },
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(match, ct);
        await _queue.EnqueueAsync(match.Id, ct);

        return Accepted(MapToResponse(match));
    }

    [HttpGet("{id:guid}/interactive-state")]
    public async Task<IActionResult> GetInteractiveState(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match == null)
            return NotFound();

        var waitingContext = _humanInput.GetWaitingContext(id);
        var completedRounds = match.Rounds.Where(r => r.Phase == RoundPhase.Completed).ToList();
        var lastRound = completedRounds.OrderByDescending(r => r.RoundNumber).FirstOrDefault();

        // Find in-progress round for spectator data
        var inProgressRound = match.Rounds
            .FirstOrDefault(r => r.Phase != RoundPhase.Completed);

        var response = new InteractiveMatchStateResponse
        {
            MatchId = match.Id,
            Status = match.Status,
            CurrentRound = completedRounds.Count + 1,
            TotalRounds = match.Config.TotalRounds,
            Scores = match.Scores,
            IsHumanTurn = waitingContext != null,
            HumanRole = waitingContext?.HumanRole,
            ExpectedInputType = waitingContext?.InputType,
            CurrentPlayerId = waitingContext?.PlayerId,
            TreasureDoor = waitingContext?.TreasureDoor,
            TrapDoor = waitingContext?.TrapDoor,
            EngineSentences = waitingContext?.EngineSentences,
            ShuffledSentences = waitingContext?.ShuffledSentences
        };

        // Populate spectator context from in-progress round
        if (inProgressRound != null)
        {
            response.CurrentArchitectId = inProgressRound.ArchitectModelId;
            response.CurrentRoundPhase = inProgressRound.Phase.ToString();

            if (inProgressRound.Phase >= RoundPhase.SentencesShuffled
                && inProgressRound.ShuffledSentences?.Any() == true)
            {
                response.SpectatorSentences = inProgressRound.ShuffledSentences
                    .Select((s, i) => new ShuffledSentenceDto
                    {
                        Index = i + 1,
                        Text = s.Text,
                        Source = s.Source.ToString()
                    })
                    .ToList();
                response.SpectatorTreasureDoor = inProgressRound.GameState.TreasureDoor.Label;
                response.SpectatorTrapDoor = inProgressRound.GameState.TrapDoor.Label;
            }
        }

        if (lastRound != null)
        {
            var playerDecisions = lastRound.PlayerDecisions
                .Where(d => d.Role == PlayerRole.Player)
                .ToList();

            response.LastRoundResult = new RoundResultSummary
            {
                RoundNumber = lastRound.RoundNumber,
                ArchitectModelId = lastRound.ArchitectModelId,
                TreasureDoor = lastRound.GameState.TreasureDoor.Label,
                TrapDoor = lastRound.GameState.TrapDoor.Label,
                PlayerChoices = playerDecisions.ToDictionary(d => d.ModelId, d => d.ChosenDoor),
                ScoreChanges = lastRound.PlayerDecisions.ToDictionary(d => d.ModelId, d => d.ScoreChange),
                Sentences = lastRound.ShuffledSentences?.Select((s, i) => new ShuffledSentenceDto
                {
                    Index = i + 1,
                    Text = s.Text,
                    Source = s.Source.ToString()
                }).ToList() ?? new()
            };
        }

        return Ok(response);
    }

    [HttpPost("{id:guid}/human-input")]
    public async Task<IActionResult> SubmitHumanInput(
        Guid id, [FromQuery] string playerId, [FromBody] SubmitHumanInputRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return BadRequest(new { Error = "playerId query parameter is required." });

        var match = await _repository.GetByIdAsync(id, ct);
        if (match == null)
            return NotFound();

        if (match.Status != MatchStatus.WaitingForHumanInput)
            return Conflict(new { Error = "Match is not waiting for human input." });

        // Validate that the playerId matches the currently waiting player
        var waitingContext = _humanInput.GetWaitingContext(id);
        if (waitingContext?.PlayerId != playerId)
            return Conflict(new { Error = $"Match is not waiting for player '{playerId}'." });

        // Validate based on input type
        if (request.InputType == PlayerRole.Architect)
        {
            if (request.ArchitectSentences == null || request.ArchitectSentences.Count == 0)
                return BadRequest(new { Error = "Architect must provide at least 1 sentence." });
            if (request.ArchitectSentences.Count > 3)
                return BadRequest(new { Error = "Architect can provide at most 3 sentences." });
        }
        else if (request.InputType == PlayerRole.Player)
        {
            if (request.ChosenDoor == null || request.ChosenDoor < 'A' || request.ChosenDoor > 'E')
                return BadRequest(new { Error = "Player must choose a door between A and E." });
        }

        var humanInput = new HumanInput
        {
            InputType = request.InputType,
            ArchitectSentences = request.ArchitectSentences,
            ChosenDoor = request.ChosenDoor,
            Reasoning = request.Reasoning
        };

        var accepted = _humanInput.SubmitInput(id, playerId, humanInput);
        if (!accepted)
            return Conflict(new { Error = "No pending input request for this match." });

        return Ok(new { Message = "Input accepted." });
    }
}
