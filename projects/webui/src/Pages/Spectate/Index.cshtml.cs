using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Spectate;

public class IndexModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public IndexModel(IBenchmarkApiClient api) => _api = api;

    public List<MatchResponse> LiveMatches { get; set; } = new();
    public List<MatchResponse> RecentMatches { get; set; } = new();

    public async Task OnGetAsync()
    {
        var matchList = await _api.GetMatchesAsync();

        LiveMatches = matchList.Matches
            .Where(m => m.IsInteractive && m.Status is
                MatchStatus.Running or MatchStatus.Pending or MatchStatus.WaitingForHumanInput)
            .OrderByDescending(m => m.StartedAt ?? m.CreatedAt)
            .ToList();

        RecentMatches = matchList.Matches
            .Where(m => m.IsInteractive && m.Status is MatchStatus.Completed)
            .OrderByDescending(m => m.CompletedAt)
            .Take(5)
            .ToList();
    }
}
