using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Models;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages;

public class LeaderboardModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public LeaderboardModel(IBenchmarkApiClient api)
    {
        _api = api;
    }

    public List<LeaderboardEntry> Entries { get; set; } = new();

    public async Task OnGetAsync()
    {
        var response = await _api.GetLeaderboardAsync();
        Entries = response.Entries;
    }
}
