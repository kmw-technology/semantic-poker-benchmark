using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.Models;

public class Sentence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public SentenceSource Source { get; set; }
    public bool IsTruthful { get; set; }
    public int OriginalIndex { get; set; }
    public int ShuffledIndex { get; set; }
    public string? TemplateId { get; set; }
}
