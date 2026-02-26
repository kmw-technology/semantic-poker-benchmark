using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.Models;

public class PlayerDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
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

    // Debug/trace fields — saved to DB for post-game analysis
    public string? SystemPrompt { get; set; }
    public string? UserPrompt { get; set; }
    public string? ParseStrategy { get; set; }
    public bool ParseSuccess { get; set; } = true;
    public string? FinishReason { get; set; }
}
