using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Infrastructure.LlmAdapters;

public class OllamaAdapter : ILlmAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaAdapter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaAdapter> logger)
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
        // Strip dedup suffix like "#2", "#3" appended for duplicate model IDs
        var hashIndex = modelId.IndexOf('#');
        if (hashIndex >= 0)
            modelId = modelId[..hashIndex];
        var client = _httpClientFactory.CreateClient("Ollama");
        var sw = Stopwatch.StartNew();

        var maxRetries = _configuration.GetValue<int>("Ollama:MaxRetries", 3);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var request = new
                {
                    model = modelId,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    stream = false,
                    options = new
                    {
                        temperature = options.Temperature,
                        num_predict = options.MaxTokens
                    }
                };

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

                _logger.LogDebug("Ollama response from {Model}: {Length} chars in {Time}ms (attempt {Attempt})",
                    modelId, content.Length, sw.ElapsedMilliseconds, attempt);

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
                _logger.LogWarning(ex, "Ollama request failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s",
                    attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }

        throw new InvalidOperationException($"Ollama request to model '{modelId}' failed after {maxRetries} attempts");
    }

    public async Task<List<ModelInfo>> ListAvailableModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Ollama");
            var response = await client.GetFromJsonAsync<JsonElement>("/api/tags", ct);
            var models = new List<ModelInfo>();

            if (response.TryGetProperty("models", out var modelsArray))
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    var name = model.GetProperty("name").GetString() ?? "";
                    var displayName = name;

                    long? parameterCount = null;
                    if (model.TryGetProperty("details", out var details) &&
                        details.TryGetProperty("parameter_size", out var paramSize))
                    {
                        var sizeStr = paramSize.GetString() ?? "";
                        parameterCount = ParseParameterSize(sizeStr);
                    }

                    models.Add(new ModelInfo
                    {
                        ModelId = name,
                        DisplayName = displayName,
                        Provider = "Ollama",
                        IsAvailable = true,
                        ParameterCount = parameterCount
                    });
                }
            }

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Ollama models");
            return new List<ModelInfo>();
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Ollama");
            var response = await client.GetAsync("/", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static long? ParseParameterSize(string sizeStr)
    {
        if (string.IsNullOrWhiteSpace(sizeStr)) return null;

        sizeStr = sizeStr.Trim().ToUpperInvariant();
        if (sizeStr.EndsWith("B"))
        {
            var numberPart = sizeStr[..^1].Trim();
            if (double.TryParse(numberPart, out var value))
                return (long)(value * 1_000_000_000);
        }
        if (sizeStr.EndsWith("M"))
        {
            var numberPart = sizeStr[..^1].Trim();
            if (double.TryParse(numberPart, out var value))
                return (long)(value * 1_000_000);
        }

        return null;
    }
}
