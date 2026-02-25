using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Api.Infrastructure.Persistence.Entities;

public class MatchEntity
{
    public Guid Id { get; set; }
    public MatchStatus Status { get; set; }
    public int TotalRounds { get; set; }
    public int CompletedRounds { get; set; }
    public string ModelIdsJson { get; set; } = "[]";
    public string ScoresJson { get; set; } = "{}";
    public bool RotateArchitect { get; set; }
    public bool AdaptivePlay { get; set; }
    public int? RandomSeed { get; set; }
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public int LlmTimeoutSeconds { get; set; } = 120;
    public double LlmTemperature { get; set; } = 0.7;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public List<RoundEntity> Rounds { get; set; } = new();
}
