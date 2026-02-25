using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.WebUI.Services;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;

namespace SemanticPoker.WebUI.Pages;

public class IndexModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public IndexModel(IBenchmarkApiClient api)
    {
        _api = api;
    }

    public HealthCheckResponse Health { get; set; } = new();
    public List<MatchResponse> RecentMatches { get; set; } = new();
    public List<MatchResponse> ActiveMatches { get; set; } = new();
    public int TotalMatches { get; set; }
    public int TotalCompletedRounds { get; set; }

    public async Task OnGetAsync()
    {
        Health = await _api.GetHealthAsync();

        var matchList = await _api.GetMatchesAsync();
        RecentMatches = matchList.Matches.Take(10).ToList();
        ActiveMatches = matchList.Matches
            .Where(m => m.Status is MatchStatus.Running or MatchStatus.Pending)
            .ToList();
        TotalMatches = matchList.Matches.Count;
        TotalCompletedRounds = matchList.Matches.Sum(m => m.CompletedRounds);
    }
}
