namespace SemanticPoker.Shared.DTOs.Responses;

public class TestPromptResponse
{
    public string Response { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public double ResponseTimeMs { get; set; }
}
