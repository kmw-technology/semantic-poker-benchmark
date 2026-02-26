namespace SemanticPoker.Shared.DTOs.Requests;

public class CreateInteractiveMatchRequest
{
    public int TotalRounds { get; set; } = 10;
    public List<PlayerSlot> Players { get; set; } = new();
    public bool RotateArchitect { get; set; } = true;
    public bool AdaptivePlay { get; set; } = true;
    public int? RandomSeed { get; set; }
    public string? OllamaBaseUrl { get; set; }
    public int? LlmTimeoutSeconds { get; set; }
    public double? LlmTemperature { get; set; }
}
