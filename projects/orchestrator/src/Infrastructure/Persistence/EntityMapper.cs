using System.Text.Json;
using SemanticPoker.Api.Infrastructure.Persistence.Entities;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Infrastructure.Persistence;

public static class EntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static MatchEntity ToEntity(Match match)
    {
        return new MatchEntity
        {
            Id = match.Id,
            Status = match.Status,
            TotalRounds = match.Config.TotalRounds,
            CompletedRounds = match.Rounds.Count(r => r.Phase == RoundPhase.Completed),
            ModelIdsJson = JsonSerializer.Serialize(match.Config.ModelIds, JsonOptions),
            ScoresJson = JsonSerializer.Serialize(match.Scores, JsonOptions),
            RotateArchitect = match.Config.RotateArchitect,
            AdaptivePlay = match.Config.AdaptivePlay,
            RandomSeed = match.Config.RandomSeed,
            OllamaBaseUrl = match.Config.OllamaBaseUrl,
            LlmTimeoutSeconds = match.Config.LlmTimeoutSeconds,
            LlmTemperature = match.Config.LlmTemperature,
            CreatedAt = match.CreatedAt,
            StartedAt = match.StartedAt,
            CompletedAt = match.CompletedAt,
            ErrorMessage = match.ErrorMessage,
            IsInteractive = match.Config.IsInteractive,
            HumanPlayerNamesJson = JsonSerializer.Serialize(match.Config.HumanPlayerNames, JsonOptions),
            PlayerPseudonymsJson = JsonSerializer.Serialize(match.Config.PlayerPseudonyms, JsonOptions),
            Rounds = match.Rounds.Select(ToEntity).ToList()
        };
    }

    public static Match ToDomain(MatchEntity entity)
    {
        return new Match
        {
            Id = entity.Id,
            Status = entity.Status,
            Config = new MatchConfig
            {
                TotalRounds = entity.TotalRounds,
                ModelIds = JsonSerializer.Deserialize<List<string>>(entity.ModelIdsJson, JsonOptions) ?? new(),
                RotateArchitect = entity.RotateArchitect,
                AdaptivePlay = entity.AdaptivePlay,
                RandomSeed = entity.RandomSeed,
                OllamaBaseUrl = entity.OllamaBaseUrl,
                LlmTimeoutSeconds = entity.LlmTimeoutSeconds,
                LlmTemperature = entity.LlmTemperature,
                IsInteractive = entity.IsInteractive,
                HumanPlayerNames = JsonSerializer.Deserialize<List<string>>(entity.HumanPlayerNamesJson, JsonOptions) ?? new(),
                PlayerPseudonyms = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.PlayerPseudonymsJson, JsonOptions) ?? new()
            },
            Scores = JsonSerializer.Deserialize<Dictionary<string, int>>(entity.ScoresJson, JsonOptions) ?? new(),
            CreatedAt = entity.CreatedAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            ErrorMessage = entity.ErrorMessage,
            Rounds = entity.Rounds.Select(ToDomain).ToList()
        };
    }

    public static RoundEntity ToEntity(Round round)
    {
        return new RoundEntity
        {
            Id = round.Id,
            RoundNumber = round.RoundNumber,
            Phase = round.Phase,
            ArchitectModelId = round.ArchitectModelId,
            GameStateJson = JsonSerializer.Serialize(round.GameState, JsonOptions),
            StartedAt = round.StartedAt,
            CompletedAt = round.CompletedAt,
            Sentences = round.EngineSentences
                .Concat(round.ArchitectSentences)
                .Concat(round.ShuffledSentences)
                .DistinctBy(s => s.Id)
                .Select(ToEntity).ToList(),
            PlayerDecisions = round.PlayerDecisions.Select(ToEntity).ToList()
        };
    }

    public static Round ToDomain(RoundEntity entity)
    {
        var gameState = JsonSerializer.Deserialize<GameState>(entity.GameStateJson, JsonOptions) ?? new();
        var sentences = entity.Sentences.Select(ToDomain).ToList();

        return new Round
        {
            Id = entity.Id,
            RoundNumber = entity.RoundNumber,
            Phase = entity.Phase,
            ArchitectModelId = entity.ArchitectModelId,
            GameState = gameState,
            EngineSentences = sentences.Where(s => s.Source == SentenceSource.Engine).ToList(),
            ArchitectSentences = sentences.Where(s => s.Source == SentenceSource.Architect).ToList(),
            ShuffledSentences = sentences.OrderBy(s => s.ShuffledIndex).ToList(),
            PlayerDecisions = entity.PlayerDecisions.Select(ToDomain).ToList(),
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt
        };
    }

    public static SentenceEntity ToEntity(Sentence sentence)
    {
        return new SentenceEntity
        {
            Id = sentence.Id,
            Text = sentence.Text,
            Source = sentence.Source,
            IsTruthful = sentence.IsTruthful,
            OriginalIndex = sentence.OriginalIndex,
            ShuffledIndex = sentence.ShuffledIndex,
            TemplateId = sentence.TemplateId
        };
    }

    public static Sentence ToDomain(SentenceEntity entity)
    {
        return new Sentence
        {
            Id = entity.Id,
            Text = entity.Text,
            Source = entity.Source,
            IsTruthful = entity.IsTruthful,
            OriginalIndex = entity.OriginalIndex,
            ShuffledIndex = entity.ShuffledIndex,
            TemplateId = entity.TemplateId
        };
    }

    public static PlayerDecisionEntity ToEntity(PlayerDecision decision)
    {
        return new PlayerDecisionEntity
        {
            Id = decision.Id,
            ModelId = decision.ModelId,
            Role = decision.Role,
            ChosenDoor = decision.ChosenDoor,
            ScoreChange = decision.ScoreChange,
            DoorOutcome = decision.DoorOutcome,
            RawResponse = decision.RawResponse,
            Reasoning = decision.Reasoning,
            PromptTokens = decision.PromptTokens,
            CompletionTokens = decision.CompletionTokens,
            ResponseTimeMs = decision.ResponseTimeMs,
            SystemPrompt = decision.SystemPrompt,
            UserPrompt = decision.UserPrompt,
            ParseStrategy = decision.ParseStrategy,
            ParseSuccess = decision.ParseSuccess,
            FinishReason = decision.FinishReason
        };
    }

    public static PlayerDecision ToDomain(PlayerDecisionEntity entity)
    {
        return new PlayerDecision
        {
            Id = entity.Id,
            ModelId = entity.ModelId,
            Role = entity.Role,
            ChosenDoor = entity.ChosenDoor,
            ScoreChange = entity.ScoreChange,
            DoorOutcome = entity.DoorOutcome,
            RawResponse = entity.RawResponse,
            Reasoning = entity.Reasoning,
            PromptTokens = entity.PromptTokens,
            CompletionTokens = entity.CompletionTokens,
            ResponseTimeMs = entity.ResponseTimeMs,
            SystemPrompt = entity.SystemPrompt,
            UserPrompt = entity.UserPrompt,
            ParseStrategy = entity.ParseStrategy,
            ParseSuccess = entity.ParseSuccess,
            FinishReason = entity.FinishReason
        };
    }
}
