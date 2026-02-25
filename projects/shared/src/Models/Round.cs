using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.Models;

public class Round
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int RoundNumber { get; set; }
    public RoundPhase Phase { get; set; } = RoundPhase.NotStarted;
    public GameState GameState { get; set; } = new();
    public string ArchitectModelId { get; set; } = string.Empty;
    public List<Sentence> EngineSentences { get; set; } = new();
    public List<Sentence> ArchitectSentences { get; set; } = new();
    public List<Sentence> ShuffledSentences { get; set; } = new();
    public List<PlayerDecision> PlayerDecisions { get; set; } = new();
    public RoundResult? Result { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
