using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.DTOs.Responses;

public class SentenceDto
{
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
    public SentenceSource Source { get; set; }
    public bool IsTruthful { get; set; }
}

public class PlayerDecisionDto
{
    public string ModelId { get; set; } = string.Empty;
    public PlayerRole Role { get; set; }
    public char ChosenDoor { get; set; }
    public DoorType DoorOutcome { get; set; }
    public int ScoreChange { get; set; }
    public string? Reasoning { get; set; }
    public double ResponseTimeMs { get; set; }
    public string? RawResponse { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string? SystemPrompt { get; set; }
    public string? UserPrompt { get; set; }
    public string? ParseStrategy { get; set; }
    public bool ParseSuccess { get; set; }
    public string? FinishReason { get; set; }
}

public class RoundDetailResponse : RoundResponse
{
    public char TreasureDoor { get; set; }
    public char TrapDoor { get; set; }
    public List<SentenceDto> ShuffledSentences { get; set; } = new();
    public List<PlayerDecisionDto> PlayerDecisions { get; set; } = new();
}
