using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Api.Infrastructure.Persistence.Entities;

public class SentenceEntity
{
    public Guid Id { get; set; }
    public Guid RoundId { get; set; }
    public string Text { get; set; } = string.Empty;
    public SentenceSource Source { get; set; }
    public bool IsTruthful { get; set; }
    public int OriginalIndex { get; set; }
    public int ShuffledIndex { get; set; }
    public string? TemplateId { get; set; }

    public RoundEntity Round { get; set; } = null!;
}
