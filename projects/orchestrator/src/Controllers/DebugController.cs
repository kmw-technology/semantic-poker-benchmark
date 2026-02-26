using Microsoft.AspNetCore.Mvc;
using SemanticPoker.Api.Infrastructure.Persistence;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IStateGenerator _stateGenerator;
    private readonly ISentenceEngine _sentenceEngine;
    private readonly ILlmAdapter _llmAdapter;
    private readonly IMatchRepository _repository;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        IStateGenerator stateGenerator,
        ISentenceEngine sentenceEngine,
        ILlmAdapter llmAdapter,
        IMatchRepository repository,
        ILogger<DebugController> logger)
    {
        _stateGenerator = stateGenerator;
        _sentenceEngine = sentenceEngine;
        _llmAdapter = llmAdapter;
        _repository = repository;
        _logger = logger;
    }

    [HttpPost("generate-state")]
    [ProducesResponseType(typeof(GenerateStateResponse), StatusCodes.Status200OK)]
    public IActionResult GenerateState([FromBody] GenerateStateRequest request)
    {
        var state = _stateGenerator.Generate(request.Seed);

        var response = new GenerateStateResponse
        {
            Doors = state.Doors.Select(d => new DoorDto
            {
                Label = d.Label,
                Type = d.Type.ToString()
            }).ToList(),
            TreasureDoor = state.TreasureDoor.Label,
            TrapDoor = state.TrapDoor.Label,
            Seed = state.Seed
        };

        return Ok(response);
    }

    [HttpPost("generate-sentences")]
    [ProducesResponseType(typeof(GenerateSentencesResponse), StatusCodes.Status200OK)]
    public IActionResult GenerateSentences([FromBody] GenerateSentencesRequest request)
    {
        var state = _stateGenerator.Generate(request.Seed);

        var sentences = _sentenceEngine.GenerateTrueSentences(state, request.Count);

        var response = new GenerateSentencesResponse
        {
            State = new GenerateStateResponse
            {
                Doors = state.Doors.Select(d => new DoorDto
                {
                    Label = d.Label,
                    Type = d.Type.ToString()
                }).ToList(),
                TreasureDoor = state.TreasureDoor.Label,
                TrapDoor = state.TrapDoor.Label,
                Seed = state.Seed
            },
            Sentences = sentences.Select((s, i) => new SentenceDto
            {
                Index = i,
                Text = s.Text,
                Source = s.Source,
                IsTruthful = s.IsTruthful
            }).ToList()
        };

        return Ok(response);
    }

    [HttpPost("test-prompt")]
    [ProducesResponseType(typeof(TestPromptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestPrompt([FromBody] TestPromptRequest request, CancellationToken ct)
    {
        try
        {
            var options = new LlmRequestOptions
            {
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens
            };

            var llmResponse = await _llmAdapter.SendPromptAsync(
                request.ModelId, request.SystemPrompt, request.UserPrompt, options, ct);

            return Ok(new TestPromptResponse
            {
                Response = llmResponse.Content,
                PromptTokens = llmResponse.PromptTokens,
                CompletionTokens = llmResponse.CompletionTokens,
                ResponseTimeMs = llmResponse.ResponseTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test prompt failed for model {ModelId}", request.ModelId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Prompt test failed",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Full match trace — every round, every decision, every raw LLM response, every prompt.
    /// </summary>
    [HttpGet("matches/{id:guid}/trace")]
    public async Task<IActionResult> GetMatchTrace(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match == null) return NotFound();

        var trace = new
        {
            match.Id,
            Status = match.Status.ToString(),
            Config = new
            {
                match.Config.TotalRounds,
                match.Config.ModelIds,
                match.Config.RotateArchitect,
                match.Config.AdaptivePlay,
                match.Config.RandomSeed,
                match.Config.LlmTemperature,
                match.Config.LlmTimeoutSeconds,
                match.Config.IsInteractive,
                match.Config.HumanPlayerNames
            },
            match.Scores,
            match.CreatedAt,
            match.StartedAt,
            match.CompletedAt,
            match.ErrorMessage,
            Rounds = match.Rounds.OrderBy(r => r.RoundNumber).Select(r => new
            {
                r.RoundNumber,
                Phase = r.Phase.ToString(),
                r.ArchitectModelId,
                GameState = new
                {
                    TreasureDoor = r.GameState.TreasureDoor.Label,
                    TrapDoor = r.GameState.TrapDoor.Label,
                    EmptyDoors = r.GameState.EmptyDoors.Select(d => d.Label).ToList()
                },
                EngineSentences = r.EngineSentences.Select(s => new
                {
                    s.Text,
                    Source = s.Source.ToString(),
                    s.IsTruthful,
                    s.TemplateId
                }).ToList(),
                ArchitectSentences = r.ArchitectSentences.Select(s => new
                {
                    s.Text,
                    Source = s.Source.ToString(),
                    s.IsTruthful
                }).ToList(),
                ShuffledSentences = r.ShuffledSentences.Select((s, i) => new
                {
                    ShuffledIndex = i + 1,
                    s.Text,
                    Source = s.Source.ToString(),
                    s.IsTruthful,
                    s.TemplateId
                }).ToList(),
                Decisions = r.PlayerDecisions.Select(d => new
                {
                    d.ModelId,
                    Role = d.Role.ToString(),
                    d.ChosenDoor,
                    DoorOutcome = d.DoorOutcome.ToString(),
                    d.ScoreChange,
                    d.Reasoning,
                    d.RawResponse,
                    d.SystemPrompt,
                    d.UserPrompt,
                    d.ParseStrategy,
                    d.ParseSuccess,
                    d.FinishReason,
                    d.PromptTokens,
                    d.CompletionTokens,
                    d.ResponseTimeMs
                }).ToList(),
                Result = r.Result != null ? new
                {
                    r.Result.ArchitectScoreChange,
                    r.Result.PlayerScoreChanges
                } : null,
                r.StartedAt,
                r.CompletedAt
            }).ToList()
        };

        return Ok(trace);
    }

    /// <summary>
    /// LLM performance stats — parse success rates, avg response times, token usage per model.
    /// </summary>
    [HttpGet("matches/{id:guid}/llm-stats")]
    public async Task<IActionResult> GetLlmStats(Guid id, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match == null) return NotFound();

        var allDecisions = match.Rounds
            .SelectMany(r => r.PlayerDecisions)
            .Where(d => !d.ModelId.StartsWith("human:"))
            .ToList();

        var byModel = allDecisions.GroupBy(d => d.ModelId).Select(g => new
        {
            ModelId = g.Key,
            TotalDecisions = g.Count(),
            AsArchitect = g.Count(d => d.Role == PlayerRole.Architect),
            AsPlayer = g.Count(d => d.Role == PlayerRole.Player),
            ParseSuccessRate = g.Count() > 0
                ? Math.Round(100.0 * g.Count(d => d.ParseSuccess) / g.Count(), 1)
                : 0,
            ParseStrategies = g.GroupBy(d => d.ParseStrategy ?? "unknown")
                .ToDictionary(s => s.Key, s => s.Count()),
            AvgResponseTimeMs = Math.Round(g.Average(d => d.ResponseTimeMs), 0),
            TotalPromptTokens = g.Sum(d => d.PromptTokens),
            TotalCompletionTokens = g.Sum(d => d.CompletionTokens),
            TotalTokens = g.Sum(d => d.PromptTokens + d.CompletionTokens),
            FinishReasons = g.GroupBy(d => d.FinishReason ?? "unknown")
                .ToDictionary(s => s.Key, s => s.Count()),
            DoorChoiceDistribution = g.Where(d => d.Role == PlayerRole.Player)
                .GroupBy(d => d.ChosenDoor)
                .ToDictionary(s => s.Key.ToString(), s => s.Count()),
            DoorOutcomeDistribution = g.Where(d => d.Role == PlayerRole.Player)
                .GroupBy(d => d.DoorOutcome.ToString())
                .ToDictionary(s => s.Key, s => s.Count())
        }).ToList();

        var summary = new
        {
            MatchId = match.Id,
            Status = match.Status.ToString(),
            TotalRounds = match.Config.TotalRounds,
            CompletedRounds = match.Rounds.Count(r => r.Phase == RoundPhase.Completed),
            TotalDecisions = allDecisions.Count,
            OverallParseSuccessRate = allDecisions.Count > 0
                ? Math.Round(100.0 * allDecisions.Count(d => d.ParseSuccess) / allDecisions.Count, 1)
                : 0,
            TotalTokensUsed = allDecisions.Sum(d => d.PromptTokens + d.CompletionTokens),
            AvgResponseTimeMs = allDecisions.Count > 0
                ? Math.Round(allDecisions.Average(d => d.ResponseTimeMs), 0)
                : 0,
            ByModel = byModel
        };

        return Ok(summary);
    }

    /// <summary>
    /// All raw LLM responses for a match — for debugging parse failures.
    /// </summary>
    [HttpGet("matches/{id:guid}/raw-responses")]
    public async Task<IActionResult> GetRawResponses(Guid id, [FromQuery] string? modelId, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match == null) return NotFound();

        var decisions = match.Rounds
            .SelectMany(r => r.PlayerDecisions.Select(d => new
            {
                r.RoundNumber,
                d.ModelId,
                Role = d.Role.ToString(),
                d.ChosenDoor,
                DoorOutcome = d.DoorOutcome.ToString(),
                d.RawResponse,
                d.ParseStrategy,
                d.ParseSuccess,
                d.FinishReason,
                d.ResponseTimeMs,
                d.PromptTokens,
                d.CompletionTokens,
                d.Reasoning
            }))
            .Where(d => modelId == null || d.ModelId == modelId)
            .OrderBy(d => d.RoundNumber)
            .ToList();

        return Ok(new { MatchId = id, Count = decisions.Count, Decisions = decisions });
    }

    /// <summary>
    /// Full prompts and responses for a specific round — for debugging a specific interaction.
    /// </summary>
    [HttpGet("matches/{id:guid}/rounds/{roundNumber:int}/prompts")]
    public async Task<IActionResult> GetRoundPrompts(Guid id, int roundNumber, CancellationToken ct)
    {
        var match = await _repository.GetByIdAsync(id, ct);
        if (match == null) return NotFound();

        var round = match.Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round == null) return NotFound();

        var prompts = round.PlayerDecisions.Select(d => new
        {
            d.ModelId,
            Role = d.Role.ToString(),
            d.SystemPrompt,
            d.UserPrompt,
            d.RawResponse,
            d.ParseStrategy,
            d.ParseSuccess,
            d.FinishReason,
            d.ChosenDoor,
            DoorOutcome = d.DoorOutcome.ToString(),
            d.Reasoning,
            d.ResponseTimeMs,
            d.PromptTokens,
            d.CompletionTokens
        }).ToList();

        return Ok(new
        {
            MatchId = id,
            round.RoundNumber,
            Phase = round.Phase.ToString(),
            round.ArchitectModelId,
            GameState = new
            {
                TreasureDoor = round.GameState.TreasureDoor.Label,
                TrapDoor = round.GameState.TrapDoor.Label,
                EmptyDoors = round.GameState.EmptyDoors.Select(d => d.Label).ToList()
            },
            Prompts = prompts
        });
    }

    /// <summary>
    /// Parse failure summary across all matches — shows which models struggle with formatting.
    /// </summary>
    [HttpGet("parse-failures")]
    public async Task<IActionResult> GetParseFailures([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var matches = await _repository.GetAllAsync(null, ct);

        var failures = matches
            .SelectMany(m => m.Rounds.SelectMany(r =>
                r.PlayerDecisions
                    .Where(d => !d.ParseSuccess)
                    .Select(d => new
                    {
                        MatchId = m.Id,
                        r.RoundNumber,
                        d.ModelId,
                        Role = d.Role.ToString(),
                        d.ParseStrategy,
                        d.ChosenDoor,
                        DoorOutcome = d.DoorOutcome.ToString(),
                        ResponsePreview = d.RawResponse?.Length > 300
                            ? d.RawResponse[..300] + "..."
                            : d.RawResponse,
                        d.FinishReason,
                        d.ResponseTimeMs
                    })))
            .OrderByDescending(f => f.MatchId)
            .Take(limit)
            .ToList();

        var byModel = failures.GroupBy(f => f.ModelId)
            .ToDictionary(g => g.Key, g => g.Count());

        return Ok(new { TotalFailures = failures.Count, ByModel = byModel, Failures = failures });
    }
}
