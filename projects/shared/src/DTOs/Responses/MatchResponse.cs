using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.DTOs.Responses;

public class MatchResponse
{
    public Guid Id { get; set; }
    public MatchStatus Status { get; set; }
    public int TotalRounds { get; set; }
    public int CompletedRounds { get; set; }
    public List<string> ModelIds { get; set; } = new();
    public Dictionary<string, int> Scores { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsInteractive { get; set; }
    public string? HumanPlayerName { get; set; }
}
