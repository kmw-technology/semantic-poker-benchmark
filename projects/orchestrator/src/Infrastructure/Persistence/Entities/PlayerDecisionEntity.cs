using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Api.Infrastructure.Persistence.Entities;

public class PlayerDecisionEntity
{
    public Guid Id { get; set; }
    public Guid RoundId { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public PlayerRole Role { get; set; }
    public char ChosenDoor { get; set; }
    public int ScoreChange { get; set; }
    public DoorType DoorOutcome { get; set; }
    public string? RawResponse { get; set; }
    public string? Reasoning { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public double ResponseTimeMs { get; set; }

    public RoundEntity Round { get; set; } = null!;
}
