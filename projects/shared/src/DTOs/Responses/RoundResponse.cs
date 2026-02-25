using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.DTOs.Responses;

public class RoundResponse
{
    public int RoundNumber { get; set; }
    public RoundPhase Phase { get; set; }
    public string ArchitectModelId { get; set; } = string.Empty;
    public Dictionary<string, char> PlayerChoices { get; set; } = new();
    public Dictionary<string, int> ScoreChanges { get; set; } = new();
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
