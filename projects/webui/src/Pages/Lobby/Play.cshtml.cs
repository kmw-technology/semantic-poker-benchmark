using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Enums;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Lobby;

public class PlayModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public PlayModel(IBenchmarkApiClient api) => _api = api;

    public Guid MatchId { get; set; }
    public MatchResponse? Match { get; set; }
    public InteractiveMatchStateResponse? GameState { get; set; }

    [BindProperty]
    public string? Sentence1 { get; set; }

    [BindProperty]
    public string? Sentence2 { get; set; }

    [BindProperty]
    public string? Sentence3 { get; set; }

    [BindProperty]
    public char? ChosenDoor { get; set; }

    [BindProperty]
    public string? Reasoning { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        MatchId = id;
        Match = await _api.GetMatchAsync(id);
        if (Match == null) return NotFound();

        GameState = await _api.GetInteractiveStateAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostArchitectAsync(Guid id)
    {
        var sentences = new List<string>();
        if (!string.IsNullOrWhiteSpace(Sentence1)) sentences.Add(Sentence1.Trim());
        if (!string.IsNullOrWhiteSpace(Sentence2)) sentences.Add(Sentence2.Trim());
        if (!string.IsNullOrWhiteSpace(Sentence3)) sentences.Add(Sentence3.Trim());

        if (sentences.Count == 0)
        {
            ErrorMessage = "Please write at least 1 deceptive sentence.";
            return await OnGetAsync(id);
        }

        var request = new SubmitHumanInputRequest
        {
            InputType = PlayerRole.Architect,
            ArchitectSentences = sentences
        };

        var success = await _api.SubmitHumanInputAsync(id, request);
        if (!success)
        {
            ErrorMessage = "Failed to submit input. Please try again.";
            return await OnGetAsync(id);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPlayerAsync(Guid id)
    {
        if (ChosenDoor == null || ChosenDoor < 'A' || ChosenDoor > 'E')
        {
            ErrorMessage = "Please choose a door (A-E).";
            return await OnGetAsync(id);
        }

        var request = new SubmitHumanInputRequest
        {
            InputType = PlayerRole.Player,
            ChosenDoor = ChosenDoor,
            Reasoning = Reasoning?.Trim()
        };

        var success = await _api.SubmitHumanInputAsync(id, request);
        if (!success)
        {
            ErrorMessage = "Failed to submit input. Please try again.";
            return await OnGetAsync(id);
        }

        return RedirectToPage(new { id });
    }
}
