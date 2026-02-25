namespace SemanticPoker.Shared.DTOs.Responses;

public class HealthCheckResponse
{
    public bool IsHealthy { get; set; }
    public bool OllamaConnected { get; set; }
    public int AvailableModels { get; set; }
    public string DatabaseStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
