using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Matches;

public class DetailModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public DetailModel(IBenchmarkApiClient api)
    {
        _api = api;
    }

    public MatchResponse? Match { get; set; }
    public List<RoundResponse> Rounds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Match = await _api.GetMatchAsync(id);
        if (Match is null) return NotFound();

        Rounds = await _api.GetRoundsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostPauseAsync(Guid id)
    {
        await _api.PauseMatchAsync(id);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResumeAsync(Guid id)
    {
        await _api.ResumeMatchAsync(id);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        await _api.CancelMatchAsync(id);
        return RedirectToPage(new { id });
    }
}
