using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Infrastructure.LlmAdapters;

public class CompositeAdapter : ILlmAdapter
{
    private readonly OllamaAdapter _ollama;
    private readonly OpenAiAdapter _openAi;

    public CompositeAdapter(OllamaAdapter ollama, OpenAiAdapter openAi)
    {
        _ollama = ollama;
        _openAi = openAi;
    }

    public Task<LlmResponse> SendPromptAsync(
        string modelId,
        string systemPrompt,
        string userPrompt,
        LlmRequestOptions? options = null,
        CancellationToken ct = default)
    {
        var adapter = ResolveAdapter(modelId);
        return adapter.SendPromptAsync(modelId, systemPrompt, userPrompt, options, ct);
    }

    public async Task<List<ModelInfo>> ListAvailableModelsAsync(CancellationToken ct = default)
    {
        var ollamaModels = await _ollama.ListAvailableModelsAsync(ct);
        var openAiModels = await _openAi.ListAvailableModelsAsync(ct);

        var combined = new List<ModelInfo>(ollamaModels.Count + openAiModels.Count);
        combined.AddRange(ollamaModels);
        combined.AddRange(openAiModels);
        return combined;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        var ollamaHealthy = await _ollama.IsHealthyAsync(ct);
        var openAiHealthy = await _openAi.IsHealthyAsync(ct);
        return ollamaHealthy || openAiHealthy;
    }

    private ILlmAdapter ResolveAdapter(string modelId)
    {
        return modelId.StartsWith("openai:") ? _openAi : _ollama;
    }
}
