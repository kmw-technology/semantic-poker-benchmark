using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;

namespace SemanticPoker.WebUI.Services;

public interface IBenchmarkApiClient
{
    Task<MatchListResponse> GetMatchesAsync(MatchStatus? status = null);
    Task<MatchResponse> CreateMatchAsync(CreateMatchRequest request);
    Task<MatchResponse?> GetMatchAsync(Guid id);
    Task<List<RoundResponse>> GetRoundsAsync(Guid matchId);
    Task<RoundDetailResponse?> GetRoundDetailAsync(Guid matchId, int roundNumber);
    Task<MatchResponse?> PauseMatchAsync(Guid id);
    Task<MatchResponse?> ResumeMatchAsync(Guid id);
    Task<MatchResponse?> CancelMatchAsync(Guid id);
    Task<ModelListResponse> GetModelsAsync();
    Task<LeaderboardResponse> GetLeaderboardAsync();
    Task<HealthCheckResponse> GetHealthAsync();
    Task<GenerateStateResponse> GenerateStateAsync(GenerateStateRequest request);
    Task<GenerateSentencesResponse> GenerateSentencesAsync(GenerateSentencesRequest request);
    Task<TestPromptResponse> TestPromptAsync(TestPromptRequest request);
    Task<MatchResponse?> CreateInteractiveMatchAsync(CreateInteractiveMatchRequest request);
    Task<InteractiveMatchStateResponse?> GetInteractiveStateAsync(Guid matchId, string? playerId = null);
    Task<bool> SubmitHumanInputAsync(Guid matchId, string playerId, SubmitHumanInputRequest input);
}

public class BenchmarkApiClient : IBenchmarkApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<BenchmarkApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public BenchmarkApiClient(HttpClient http, ILogger<BenchmarkApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<MatchListResponse> GetMatchesAsync(MatchStatus? status = null)
    {
        var url = "/api/matches";
        if (status.HasValue) url += $"?status={status.Value}";
        return await _http.GetFromJsonAsync<MatchListResponse>(url, JsonOptions) ?? new();
    }

    public async Task<MatchResponse> CreateMatchAsync(CreateMatchRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/matches", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MatchResponse>(JsonOptions) ?? throw new Exception("Failed to create match");
    }

    public async Task<MatchResponse?> GetMatchAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<MatchResponse>($"/api/matches/{id}", JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<RoundResponse>> GetRoundsAsync(Guid matchId)
    {
        return await _http.GetFromJsonAsync<List<RoundResponse>>($"/api/matches/{matchId}/rounds", JsonOptions) ?? new();
    }

    public async Task<RoundDetailResponse?> GetRoundDetailAsync(Guid matchId, int roundNumber)
    {
        try
        {
            return await _http.GetFromJsonAsync<RoundDetailResponse>($"/api/matches/{matchId}/rounds/{roundNumber}", JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<MatchResponse?> PauseMatchAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/matches/{id}/pause", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MatchResponse>(JsonOptions);
    }

    public async Task<MatchResponse?> ResumeMatchAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/matches/{id}/resume", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MatchResponse>(JsonOptions);
    }

    public async Task<MatchResponse?> CancelMatchAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/matches/{id}/cancel", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MatchResponse>(JsonOptions);
    }

    public async Task<ModelListResponse> GetModelsAsync()
    {
        return await _http.GetFromJsonAsync<ModelListResponse>("/api/models", JsonOptions) ?? new();
    }

    public async Task<LeaderboardResponse> GetLeaderboardAsync()
    {
        return await _http.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard", JsonOptions) ?? new();
    }

    public async Task<HealthCheckResponse> GetHealthAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<HealthCheckResponse>("/api/health", JsonOptions) ?? new();
        }
        catch
        {
            return new HealthCheckResponse { IsHealthy = false, DatabaseStatus = "API Unreachable" };
        }
    }

    public async Task<GenerateStateResponse> GenerateStateAsync(GenerateStateRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/debug/generate-state", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GenerateStateResponse>(JsonOptions) ?? new();
    }

    public async Task<GenerateSentencesResponse> GenerateSentencesAsync(GenerateSentencesRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/debug/generate-sentences", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GenerateSentencesResponse>(JsonOptions) ?? new();
    }

    public async Task<TestPromptResponse> TestPromptAsync(TestPromptRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/debug/test-prompt", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TestPromptResponse>(JsonOptions) ?? new();
    }

    public async Task<MatchResponse?> CreateInteractiveMatchAsync(CreateInteractiveMatchRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/matches/interactive", request, JsonOptions);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<MatchResponse>(JsonOptions);
        return null;
    }

    public async Task<InteractiveMatchStateResponse?> GetInteractiveStateAsync(Guid matchId, string? playerId = null)
    {
        try
        {
            var url = $"/api/matches/{matchId}/interactive-state";
            if (!string.IsNullOrEmpty(playerId))
                url += $"?playerId={Uri.EscapeDataString(playerId)}";
            return await _http.GetFromJsonAsync<InteractiveMatchStateResponse>(url, JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> SubmitHumanInputAsync(Guid matchId, string playerId, SubmitHumanInputRequest input)
    {
        var encodedPlayerId = Uri.EscapeDataString(playerId);
        var response = await _http.PostAsJsonAsync(
            $"/api/matches/{matchId}/human-input?playerId={encodedPlayerId}", input, JsonOptions);
        return response.IsSuccessStatusCode;
    }
}
