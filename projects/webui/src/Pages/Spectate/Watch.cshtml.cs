using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Spectate;

public class WatchModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public WatchModel(IBenchmarkApiClient api) => _api = api;

    public Guid MatchId { get; set; }
    public MatchResponse? Match { get; set; }
    public InteractiveMatchStateResponse? GameState { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        MatchId = id;
        Match = await _api.GetMatchAsync(id);
        if (Match == null) return NotFound();

        GameState = await _api.GetInteractiveStateAsync(id);
        return Page();
    }
}
