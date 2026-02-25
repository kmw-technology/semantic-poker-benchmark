namespace SemanticPoker.Shared.Models;

public class MatchConfig
{
    public int TotalRounds { get; set; } = 10;
    public List<string> ModelIds { get; set; } = new();
    public bool RotateArchitect { get; set; } = true;
    public bool AdaptivePlay { get; set; } = true;
    public int? RandomSeed { get; set; }
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public int LlmTimeoutSeconds { get; set; } = 120;
    public double LlmTemperature { get; set; } = 0.7;
}
