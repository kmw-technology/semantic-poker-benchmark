using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.DTOs.Responses;

public class InteractiveMatchStateResponse
{
    public Guid MatchId { get; set; }
    public MatchStatus Status { get; set; }
    public int CurrentRound { get; set; }
    public int TotalRounds { get; set; }
    public Dictionary<string, int> Scores { get; set; } = new();

    public PlayerRole? HumanRole { get; set; }
    public bool IsHumanTurn { get; set; }
    public PlayerRole? ExpectedInputType { get; set; }

    // Architect context (shown when human is Architect)
    public char? TreasureDoor { get; set; }
    public char? TrapDoor { get; set; }
    public List<string>? EngineSentences { get; set; }

    // Player context (shown when human is Player)
    public List<ShuffledSentenceDto>? ShuffledSentences { get; set; }

    // Last completed round result
    public RoundResultSummary? LastRoundResult { get; set; }
}

public class ShuffledSentenceDto
{
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class RoundResultSummary
{
    public int RoundNumber { get; set; }
    public string ArchitectModelId { get; set; } = string.Empty;
    public Dictionary<string, char> PlayerChoices { get; set; } = new();
    public Dictionary<string, int> ScoreChanges { get; set; } = new();
    public char TreasureDoor { get; set; }
    public char TrapDoor { get; set; }
}
