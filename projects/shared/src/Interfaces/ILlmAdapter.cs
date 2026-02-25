using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.Interfaces;

public class LlmRequestOptions
{
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1024;
    public int TimeoutSeconds { get; set; } = 120;
}

public class LlmResponse
{
    public string Content { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public double ResponseTimeMs { get; set; }
    public string? FinishReason { get; set; }
}

public interface ILlmAdapter
{
    Task<LlmResponse> SendPromptAsync(
        string modelId,
        string systemPrompt,
        string userPrompt,
        LlmRequestOptions? options = null,
        CancellationToken ct = default);

    Task<List<ModelInfo>> ListAvailableModelsAsync(CancellationToken ct = default);

    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
