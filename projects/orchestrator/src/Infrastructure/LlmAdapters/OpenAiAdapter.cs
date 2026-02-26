using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Infrastructure.LlmAdapters;

public class OpenAiAdapter : ILlmAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly List<ModelInfo> SupportedModels = new()
    {
        new() { ModelId = "openai:gpt-5-mini:low", DisplayName = "GPT-5 Mini (Low Reasoning)", Provider = "OpenAI", IsAvailable = true },
        new() { ModelId = "openai:gpt-5-mini:medium", DisplayName = "GPT-5 Mini (Medium Reasoning)", Provider = "OpenAI", IsAvailable = true },
        new() { ModelId = "openai:gpt-5-mini:high", DisplayName = "GPT-5 Mini (High Reasoning)", Provider = "OpenAI", IsAvailable = true },
        new() { ModelId = "openai:gpt-5-nano:low", DisplayName = "GPT-5 Nano (Low Reasoning)", Provider = "OpenAI", IsAvailable = true },
        new() { ModelId = "openai:gpt-5-nano:medium", DisplayName = "GPT-5 Nano (Medium Reasoning)", Provider = "OpenAI", IsAvailable = true },
        new() { ModelId = "openai:gpt-5-nano:high", DisplayName = "GPT-5 Nano (High Reasoning)", Provider = "OpenAI", IsAvailable = true },
    };

    public OpenAiAdapter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAiAdapter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LlmResponse> SendPromptAsync(
        string modelId,
        string systemPrompt,
        string userPrompt,
        LlmRequestOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new LlmRequestOptions();
        var (baseModel, reasoningEffort) = ParseModelId(modelId);
        var client = _httpClientFactory.CreateClient("OpenAI");
        var sw = Stopwatch.StartNew();

        var maxRetries = _configuration.GetValue<int>("OpenAi:MaxRetries", 3);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var request = new Dictionary<string, object>
                {
                    ["model"] = baseModel,
                    ["messages"] = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    ["max_completion_tokens"] = options.MaxTokens
                };

                if (reasoningEffort != null)
                {
                    request["reasoning_effort"] = reasoningEffort;
                }

                // Only set temperature for non-reasoning or when explicitly needed
                if (reasoningEffort == null)
                {
                    request["temperature"] = options.Temperature;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

                var response = await client.PostAsJsonAsync("/v1/chat/completions", request, _jsonOptions, cts.Token);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cts.Token);
                sw.Stop();

                var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                var finishReason = json.GetProperty("choices")[0].GetProperty("finish_reason").GetString();

                int promptTokens = 0, completionTokens = 0;
                if (json.TryGetProperty("usage", out var usage))
                {
                    promptTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
                    completionTokens = usage.TryGetProperty("completion_tokens", out var ct2) ? ct2.GetInt32() : 0;
                }

                _logger.LogDebug("OpenAI response from {Model} (reasoning={Reasoning}): {Length} chars in {Time}ms (attempt {Attempt})",
                    baseModel, reasoningEffort ?? "default", content.Length, sw.ElapsedMilliseconds, attempt);

                return new LlmResponse
                {
                    Content = content,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    FinishReason = finishReason
                };
            }
            catch (Exception ex) when (attempt < maxRetries && !ct.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "OpenAI request failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s",
                    attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }

        throw new InvalidOperationException($"OpenAI request to model '{modelId}' failed after {maxRetries} attempts");
    }

    public Task<List<ModelInfo>> ListAvailableModelsAsync(CancellationToken ct = default)
    {
        var apiKey = _configuration["OpenAi:ApiKey"];
        var isConfigured = !string.IsNullOrWhiteSpace(apiKey);

        var models = SupportedModels
            .Select(m => new ModelInfo
            {
                ModelId = m.ModelId,
                DisplayName = m.DisplayName,
                Provider = m.Provider,
                IsAvailable = isConfigured,
                ParameterCount = m.ParameterCount
            })
            .ToList();

        return Task.FromResult(models);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var apiKey = _configuration["OpenAi:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return false;

            var client = _httpClientFactory.CreateClient("OpenAI");
            var response = await client.GetAsync("/v1/models", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses model ID format "openai:{model}:{reasoning_effort}" into base model and reasoning effort.
    /// Strips any "#N" dedup suffix first (e.g. "openai:gpt-5-mini:low#2" → "openai:gpt-5-mini:low").
    /// Example: "openai:gpt-5-mini:low" → ("gpt-5-mini", "low")
    /// </summary>
    private static (string baseModel, string? reasoningEffort) ParseModelId(string modelId)
    {
        // Strip dedup suffix like "#2", "#3" appended for duplicate model IDs
        var hashIndex = modelId.IndexOf('#');
        if (hashIndex >= 0)
            modelId = modelId[..hashIndex];

        var parts = modelId.Split(':');
        if (parts.Length >= 3 && parts[0] == "openai")
        {
            return (parts[1], parts[2]);
        }
        if (parts.Length >= 2 && parts[0] == "openai")
        {
            return (parts[1], null);
        }
        return (modelId, null);
    }
}
