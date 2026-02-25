using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Api.Infrastructure.Persistence.Entities;

public class RoundEntity
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public int RoundNumber { get; set; }
    public RoundPhase Phase { get; set; }
    public string ArchitectModelId { get; set; } = string.Empty;
    public string GameStateJson { get; set; } = "{}";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public MatchEntity Match { get; set; } = null!;
    public List<SentenceEntity> Sentences { get; set; } = new();
    public List<PlayerDecisionEntity> PlayerDecisions { get; set; } = new();
}
