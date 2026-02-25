using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.Models;

public class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MatchConfig Config { get; set; } = new();
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public List<Round> Rounds { get; set; } = new();
    public Dictionary<string, int> Scores { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public Round? CurrentRound => Rounds.LastOrDefault(r => r.Phase != RoundPhase.Completed)
                                  ?? Rounds.LastOrDefault();
}
