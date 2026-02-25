namespace SemanticPoker.Shared.Models;

public class RoundResult
{
    public int ArchitectScoreChange { get; set; }
    public Dictionary<string, int> PlayerScoreChanges { get; set; } = new();
}
