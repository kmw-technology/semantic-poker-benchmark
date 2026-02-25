using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Matches;

public class RoundDetailModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public RoundDetailModel(IBenchmarkApiClient api)
    {
        _api = api;
    }

    public Guid MatchId { get; set; }
    public RoundDetailResponse? Round { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid matchId, int roundNumber)
    {
        MatchId = matchId;
        Round = await _api.GetRoundDetailAsync(matchId, roundNumber);
        if (Round is null) return NotFound();
        return Page();
    }
}
