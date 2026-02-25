using System.Net.Http.Json;
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
}

public class BenchmarkApiClient : IBenchmarkApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<BenchmarkApiClient> _logger;

    public BenchmarkApiClient(HttpClient http, ILogger<BenchmarkApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<MatchListResponse> GetMatchesAsync(MatchStatus? status = null)
    {
        var url = "/api/matches";
        if (status.HasValue) url += $"?status={status.Value}";
        return await _http.GetFromJsonAsync<MatchListResponse>(url) ?? new();
    }

    public async Task<MatchResponse> CreateMatchAsync(CreateMatchRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/matches", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MatchResponse>() ?? throw new Exception("Failed to create match");
    }

    public async Task<MatchResponse?> GetMatchAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<MatchResponse>($"/api/matches/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<RoundResponse>> GetRoundsAsync(Guid matchId)
    {
        return await _http.GetFromJsonAsync<List<RoundResponse>>($"/api/matches/{matchId}/rounds") ?? new();
    }

    public async Task<RoundDetailResponse?> GetRoundDetailAsync(Guid matchId, int roundNumber)
    {
        try
        {
            return await _http.GetFromJsonAsync<RoundDetailResponse>($"/api/matches/{matchId}/rounds/{roundNumber}");
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
        return await response.Content.ReadFromJsonAsync<MatchResponse>();
    }

    public async Task<MatchResponse?> ResumeMatchAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/matches/{id}/resume", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MatchResponse>();
    }

    public async Task<MatchResponse?> CancelMatchAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/matches/{id}/cancel", null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MatchResponse>();
    }

    public async Task<ModelListResponse> GetModelsAsync()
    {
        return await _http.GetFromJsonAsync<ModelListResponse>("/api/models") ?? new();
    }

    public async Task<LeaderboardResponse> GetLeaderboardAsync()
    {
        return await _http.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard") ?? new();
    }

    public async Task<HealthCheckResponse> GetHealthAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<HealthCheckResponse>("/api/health") ?? new();
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
        return await response.Content.ReadFromJsonAsync<GenerateStateResponse>() ?? new();
    }

    public async Task<GenerateSentencesResponse> GenerateSentencesAsync(GenerateSentencesRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/debug/generate-sentences", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GenerateSentencesResponse>() ?? new();
    }

    public async Task<TestPromptResponse> TestPromptAsync(TestPromptRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/debug/test-prompt", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TestPromptResponse>() ?? new();
    }
}
