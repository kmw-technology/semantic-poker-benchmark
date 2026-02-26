using Microsoft.AspNetCore.SignalR;
using SemanticPoker.Api.Hubs;
using SemanticPoker.Api.Infrastructure.Persistence;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Services;

public class MatchRunnerService
{
    private readonly IMatchRepository _repository;
    private readonly IStateGenerator _stateGenerator;
    private readonly ISentenceEngine _sentenceEngine;
    private readonly IScoreCalculator _scoreCalculator;
    private readonly ILlmAdapter _llmAdapter;
    private readonly PromptBuilder _promptBuilder;
    private readonly LlmResponseParser _responseParser;
    private readonly AdaptiveHistoryBuilder _historyBuilder;
    private readonly ArchitectRotation _architectRotation;
    private readonly IHubContext<MatchProgressHub> _hubContext;
    private readonly ILogger<MatchRunnerService> _logger;
    private readonly HumanInputCoordinator _humanInput;

    public MatchRunnerService(
        IMatchRepository repository,
        IStateGenerator stateGenerator,
        ISentenceEngine sentenceEngine,
        IScoreCalculator scoreCalculator,
        ILlmAdapter llmAdapter,
        PromptBuilder promptBuilder,
        LlmResponseParser responseParser,
        AdaptiveHistoryBuilder historyBuilder,
        ArchitectRotation architectRotation,
        IHubContext<MatchProgressHub> hubContext,
        ILogger<MatchRunnerService> logger,
        HumanInputCoordinator humanInput)
    {
        _repository = repository;
        _stateGenerator = stateGenerator;
        _sentenceEngine = sentenceEngine;
        _scoreCalculator = scoreCalculator;
        _llmAdapter = llmAdapter;
        _promptBuilder = promptBuilder;
        _responseParser = responseParser;
        _historyBuilder = historyBuilder;
        _architectRotation = architectRotation;
        _hubContext = hubContext;
        _logger = logger;
        _humanInput = humanInput;
    }

    private static bool IsHumanPlayer(string modelId) => modelId.StartsWith("human:");

    public async Task RunMatchAsync(Guid matchId, CancellationToken ct = default)
    {
        var match = await _repository.GetByIdAsync(matchId, ct);
        if (match is null)
        {
            _logger.LogError("Match {MatchId} not found", matchId);
            return;
        }

        try
        {
            match.Status = MatchStatus.Running;
            match.StartedAt = DateTime.UtcNow;

            // Initialize scores
            foreach (var modelId in match.Config.ModelIds)
            {
                if (!match.Scores.ContainsKey(modelId))
                    match.Scores[modelId] = 0;
            }

            await _repository.UpdateAsync(match, ct);

            var startRound = match.Rounds.Count(r => r.Phase == RoundPhase.Completed) + 1;

            for (int roundNum = startRound; roundNum <= match.Config.TotalRounds; roundNum++)
            {
                ct.ThrowIfCancellationRequested();

                // Check for pause/cancel between rounds
                var freshMatch = await _repository.GetByIdAsync(matchId, ct);
                if (freshMatch?.Status == MatchStatus.Paused)
                {
                    _logger.LogInformation("Match {MatchId} paused at round {Round}", matchId, roundNum);
                    return;
                }
                if (freshMatch?.Status == MatchStatus.Cancelled)
                {
                    _logger.LogInformation("Match {MatchId} cancelled at round {Round}", matchId, roundNum);
                    return;
                }

                _logger.LogInformation("Starting round {Round}/{Total} for match {MatchId}",
                    roundNum, match.Config.TotalRounds, matchId);

                var round = await RunRoundAsync(match, roundNum, ct);

                // Update scores
                if (round.Result != null)
                {
                    var architectId = round.ArchitectModelId;
                    match.Scores[architectId] = match.Scores.GetValueOrDefault(architectId) + round.Result.ArchitectScoreChange;

                    foreach (var (playerId, scoreChange) in round.Result.PlayerScoreChanges)
                    {
                        match.Scores[playerId] = match.Scores.GetValueOrDefault(playerId) + scoreChange;
                    }
                }

                await _repository.UpdateAsync(match, ct);

                await _hubContext.Clients.Group(matchId.ToString()).SendAsync("RoundCompleted", new
                {
                    MatchId = matchId,
                    RoundNumber = roundNum,
                    CompletedRounds = match.Rounds.Count(r => r.Phase == RoundPhase.Completed),
                    TotalRounds = match.Config.TotalRounds,
                    Scores = match.Scores,
                    Status = match.Status.ToString()
                }, ct);
            }

            match.Status = MatchStatus.Completed;
            match.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(match, ct);

            await _hubContext.Clients.Group(matchId.ToString()).SendAsync("MatchCompleted", new
            {
                MatchId = matchId,
                Scores = match.Scores,
                Status = match.Status.ToString()
            }, ct);

            _logger.LogInformation("Match {MatchId} completed. Scores: {Scores}", matchId,
                string.Join(", ", match.Scores.Select(kv => $"{kv.Key}: {kv.Value}")));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Match {MatchId} was cancelled", matchId);
            match.Status = MatchStatus.Cancelled;
            await _repository.UpdateAsync(match, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Match {MatchId} failed with error", matchId);
            match.Status = MatchStatus.Failed;
            match.ErrorMessage = ex.Message;
            await _repository.UpdateAsync(match, CancellationToken.None);
        }
    }

    private async Task<Round> RunRoundAsync(Match match, int roundNumber, CancellationToken ct)
    {
        var round = new Round
        {
            RoundNumber = roundNumber,
            Phase = RoundPhase.NotStarted,
            StartedAt = DateTime.UtcNow
        };
        match.Rounds.Add(round);

        // Phase 1: Generate state
        var seed = match.Config.RandomSeed.HasValue
            ? match.Config.RandomSeed.Value + roundNumber
            : (int?)null;
        round.GameState = _stateGenerator.Generate(seed);
        round.Phase = RoundPhase.StateGenerated;

        _logger.LogDebug("Round {Round}: State generated - Treasure={Treasure}, Trap={Trap}",
            roundNumber, round.GameState.TreasureDoor.Label, round.GameState.TrapDoor.Label);

        // Phase 2: Engine sentences
        round.EngineSentences = _sentenceEngine.GenerateTrueSentences(round.GameState, 2);
        round.Phase = RoundPhase.EngineSentencesGenerated;

        _logger.LogDebug("Round {Round}: Engine sentences: {Sentences}",
            roundNumber, string.Join(" | ", round.EngineSentences.Select(s => s.Text)));

        // Phase 3: Architect sentences
        var architectModelId = match.Config.RotateArchitect
            ? _architectRotation.GetArchitectModelId(roundNumber, match.Config.ModelIds)
            : match.Config.ModelIds[0];
        round.ArchitectModelId = architectModelId;

        var history = match.Config.AdaptivePlay
            ? _historyBuilder.BuildArchitectHistory(match.Rounds)
            : null;

        var architectSystemPrompt = _promptBuilder.BuildArchitectSystemPrompt();
        var architectUserPrompt = _promptBuilder.BuildArchitectUserPrompt(
            round.GameState, round.EngineSentences, history);

        var llmOptions = new LlmRequestOptions
        {
            Temperature = match.Config.LlmTemperature,
            MaxTokens = 1024,
            TimeoutSeconds = match.Config.LlmTimeoutSeconds
        };

        if (IsHumanPlayer(architectModelId))
        {
            // Set status to waiting and persist
            match.Status = MatchStatus.WaitingForHumanInput;
            await _repository.UpdateAsync(match, ct);

            // Notify via SignalR
            await _hubContext.Clients.Group(match.Id.ToString()).SendAsync("WaitingForHumanInput", new
            {
                MatchId = match.Id,
                RoundNumber = roundNumber,
                HumanRole = "Architect",
                PlayerId = architectModelId,
                TreasureDoor = round.GameState.TreasureDoor.Label,
                TrapDoor = round.GameState.TrapDoor.Label,
                EngineSentences = round.EngineSentences.Select(s => s.Text).ToList()
            }, ct);

            // Wait for human input
            var humanInput = await _humanInput.WaitForHumanInputAsync(match.Id, architectModelId, new HumanInputRequest
            {
                InputType = PlayerRole.Architect,
                RoundNumber = roundNumber,
                HumanRole = PlayerRole.Architect,
                TreasureDoor = round.GameState.TreasureDoor.Label,
                TrapDoor = round.GameState.TrapDoor.Label,
                EngineSentences = round.EngineSentences.Select(s => s.Text).ToList()
            }, ct);

            // Resume
            match.Status = MatchStatus.Running;
            await _repository.UpdateAsync(match, ct);

            // Build sentences from human input
            var humanSentences = (humanInput.ArchitectSentences ?? new List<string>())
                .Take(3)
                .Select((text, i) => new Sentence
                {
                    Id = Guid.NewGuid(),
                    Text = text,
                    Source = SentenceSource.Architect,
                    IsTruthful = false,
                    OriginalIndex = i,
                    ShuffledIndex = -1
                })
                .ToList();

            round.ArchitectSentences = humanSentences;

            // Record architect decision
            round.PlayerDecisions.Add(new PlayerDecision
            {
                Id = Guid.NewGuid(),
                ModelId = architectModelId,
                Role = PlayerRole.Architect,
                ChosenDoor = '\0',
                RawResponse = string.Join(" | ", humanInput.ArchitectSentences ?? new()),
                PromptTokens = 0,
                CompletionTokens = 0,
                ResponseTimeMs = 0
            });
        }
        else
        {
            var architectResponse = await _llmAdapter.SendPromptAsync(
                architectModelId, architectSystemPrompt, architectUserPrompt, llmOptions, ct);

            var parseResult = _responseParser.ParseArchitectSentencesWithMeta(architectResponse.Content);
            round.ArchitectSentences = parseResult.Sentences;

            _logger.LogInformation("Round {Round}: Architect {Model} parse: strategy={Strategy}, success={Success}, sentences={Count}, responseTime={Time}ms, tokens={Prompt}+{Completion}",
                roundNumber, architectModelId, parseResult.Strategy, parseResult.Success,
                parseResult.Sentences.Count, architectResponse.ResponseTimeMs,
                architectResponse.PromptTokens, architectResponse.CompletionTokens);

            // Store architect decision with full trace data
            round.PlayerDecisions.Add(new PlayerDecision
            {
                ModelId = architectModelId,
                Role = PlayerRole.Architect,
                ChosenDoor = '\0',
                RawResponse = architectResponse.Content,
                PromptTokens = architectResponse.PromptTokens,
                CompletionTokens = architectResponse.CompletionTokens,
                ResponseTimeMs = architectResponse.ResponseTimeMs,
                SystemPrompt = architectSystemPrompt,
                UserPrompt = architectUserPrompt,
                ParseStrategy = parseResult.Strategy,
                ParseSuccess = parseResult.Success,
                FinishReason = architectResponse.FinishReason
            });
        }

        round.Phase = RoundPhase.ArchitectSentencesGenerated;

        _logger.LogInformation("Round {Round}: Architect {Model} wrote {Count} sentences: {Sentences}",
            roundNumber, architectModelId, round.ArchitectSentences.Count,
            string.Join(" | ", round.ArchitectSentences.Select(s => s.Text)));

        // Phase 4: Shuffle sentences
        round.ShuffledSentences = _sentenceEngine.ShuffleSentences(
            round.EngineSentences, round.ArchitectSentences, seed);
        round.Phase = RoundPhase.SentencesShuffled;

        // Persist for spectators: doors + sentences are now visible
        await _repository.UpdateAsync(match, ct);

        await _hubContext.Clients.Group(match.Id.ToString()).SendAsync("PlayersDeciding", new
        {
            MatchId = match.Id,
            RoundNumber = roundNumber,
            ArchitectModelId = architectModelId
        }, ct);

        // Phase 5: Player decisions
        round.Phase = RoundPhase.PlayersDeciding;
        var playerModelIds = match.Config.RotateArchitect
            ? _architectRotation.GetPlayerModelIds(roundNumber, match.Config.ModelIds)
            : match.Config.ModelIds.Where(m => m != architectModelId).ToList();

        var playerSystemPrompt = _promptBuilder.BuildPlayerSystemPrompt();

        foreach (var playerModelId in playerModelIds)
        {
            if (IsHumanPlayer(playerModelId))
            {
                // Set status to waiting
                match.Status = MatchStatus.WaitingForHumanInput;
                await _repository.UpdateAsync(match, ct);

                // Notify via SignalR
                await _hubContext.Clients.Group(match.Id.ToString()).SendAsync("WaitingForHumanInput", new
                {
                    MatchId = match.Id,
                    RoundNumber = roundNumber,
                    HumanRole = "Player",
                    PlayerId = playerModelId,
                    ShuffledSentences = round.ShuffledSentences.Select((s, i) => new { Index = i + 1, s.Text }).ToList()
                }, ct);

                // Wait for human input
                var humanInput = await _humanInput.WaitForHumanInputAsync(match.Id, playerModelId, new HumanInputRequest
                {
                    InputType = PlayerRole.Player,
                    RoundNumber = roundNumber,
                    HumanRole = PlayerRole.Player,
                    ShuffledSentences = round.ShuffledSentences.Select((s, i) => new ShuffledSentenceDto
                    {
                        Index = i + 1,
                        Text = s.Text
                    }).ToList()
                }, ct);

                // Resume
                match.Status = MatchStatus.Running;
                await _repository.UpdateAsync(match, ct);

                var chosenDoor = humanInput.ChosenDoor ?? 'C';
                var reasoning = humanInput.Reasoning;
                var doorType = round.GameState.Doors.FirstOrDefault(d => d.Label == chosenDoor)?.Type ?? DoorType.Empty;

                var decision = new PlayerDecision
                {
                    Id = Guid.NewGuid(),
                    ModelId = playerModelId,
                    Role = PlayerRole.Player,
                    ChosenDoor = chosenDoor,
                    DoorOutcome = doorType,
                    RawResponse = $"DOOR: {chosenDoor}",
                    Reasoning = reasoning,
                    PromptTokens = 0,
                    CompletionTokens = 0,
                    ResponseTimeMs = 0
                };
                round.PlayerDecisions.Add(decision);
            }
            else
            {
                var playerHistory = match.Config.AdaptivePlay
                    ? _historyBuilder.BuildPlayerHistory(match.Rounds, playerModelId)
                    : null;

                var playerUserPrompt = _promptBuilder.BuildPlayerUserPrompt(
                    round.ShuffledSentences, playerHistory);

                var playerResponse = await _llmAdapter.SendPromptAsync(
                    playerModelId, playerSystemPrompt, playerUserPrompt, llmOptions, ct);

                var parseResult = _responseParser.ParsePlayerResponseWithMeta(playerResponse.Content);

                var doorType = round.GameState.Doors.FirstOrDefault(d => d.Label == parseResult.Door)?.Type ?? DoorType.Empty;

                round.PlayerDecisions.Add(new PlayerDecision
                {
                    ModelId = playerModelId,
                    Role = PlayerRole.Player,
                    ChosenDoor = parseResult.Door,
                    DoorOutcome = doorType,
                    RawResponse = playerResponse.Content,
                    Reasoning = parseResult.Reasoning,
                    PromptTokens = playerResponse.PromptTokens,
                    CompletionTokens = playerResponse.CompletionTokens,
                    ResponseTimeMs = playerResponse.ResponseTimeMs,
                    SystemPrompt = playerSystemPrompt,
                    UserPrompt = playerUserPrompt,
                    ParseStrategy = parseResult.Strategy,
                    ParseSuccess = parseResult.Success,
                    FinishReason = playerResponse.FinishReason
                });

                _logger.LogInformation("Round {Round}: Player {Model} chose Door {Door} ({Outcome}), parse: strategy={Strategy}, success={Success}, time={Time}ms, tokens={Prompt}+{Completion}",
                    roundNumber, playerModelId, parseResult.Door, doorType,
                    parseResult.Strategy, parseResult.Success,
                    playerResponse.ResponseTimeMs, playerResponse.PromptTokens, playerResponse.CompletionTokens);
            }
        }

        // Phase 6: Scoring
        round.Phase = RoundPhase.Scoring;
        var playerDecisions = round.PlayerDecisions.Where(d => d.Role == PlayerRole.Player).ToList();
        round.Result = _scoreCalculator.CalculateRound(round.GameState, playerDecisions, architectModelId);

        // Update individual decision scores
        foreach (var decision in round.PlayerDecisions.Where(d => d.Role == PlayerRole.Player))
        {
            if (round.Result.PlayerScoreChanges.TryGetValue(decision.ModelId, out var scoreChange))
                decision.ScoreChange = scoreChange;
        }

        // Update architect decision score
        var architectDecision = round.PlayerDecisions.FirstOrDefault(d => d.Role == PlayerRole.Architect);
        if (architectDecision != null)
            architectDecision.ScoreChange = round.Result.ArchitectScoreChange;

        round.Phase = RoundPhase.Completed;
        round.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation("Round {Round}: Completed. Architect score: {ArchitectScore}. Player scores: {PlayerScores}",
            roundNumber, round.Result.ArchitectScoreChange,
            string.Join(", ", round.Result.PlayerScoreChanges.Select(kv => $"{kv.Key}:{kv.Value}")));

        return round;
    }
}
