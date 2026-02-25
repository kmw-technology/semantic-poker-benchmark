namespace SemanticPoker.Shared.Models;

public class ModelInfo
{
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public long? ParameterCount { get; set; }
}
