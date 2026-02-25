namespace SemanticPoker.Shared.Models;

public class RoundHistorySummary
{
    public int RoundNumber { get; set; }
    public string ArchitectModelId { get; set; } = string.Empty;
    public Dictionary<string, char> PlayerChoices { get; set; } = new();
    public Dictionary<string, string> Outcomes { get; set; } = new();
    public List<string> ShuffledSentenceTexts { get; set; } = new();
}
