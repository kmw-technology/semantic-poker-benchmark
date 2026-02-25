namespace SemanticPoker.Shared.DTOs.Requests;

public class TestPromptRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1024;
}
