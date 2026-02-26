using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Replay;

public class IndexModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public IndexModel(IBenchmarkApiClient api)
    {
        _api = api;
    }

    public MatchResponse Match { get; set; } = default!;
    public RoundDetailResponse Round { get; set; } = default!;
    public List<RoundResponse> AllRounds { get; set; } = new();
    public int CurrentRound { get; set; }
    public int CompletedRounds { get; set; }
    public Dictionary<string, int> RunningScores { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, int round = 1)
    {
        var match = await _api.GetMatchAsync(id);
        if (match == null) return NotFound();
        Match = match;

        AllRounds = await _api.GetRoundsAsync(id);
        CompletedRounds = AllRounds.Count(r => r.Phase == RoundPhase.Completed);
        if (CompletedRounds == 0) return NotFound();

        CurrentRound = Math.Clamp(round, 1, CompletedRounds);

        var roundDetail = await _api.GetRoundDetailAsync(id, CurrentRound);
        if (roundDetail == null) return NotFound();
        Round = roundDetail;

        // Compute running scores up to current round
        foreach (var r in AllRounds.Where(r => r.RoundNumber <= CurrentRound))
            foreach (var kv in r.ScoreChanges)
                RunningScores[kv.Key] = RunningScores.GetValueOrDefault(kv.Key) + kv.Value;

        return Page();
    }
}
