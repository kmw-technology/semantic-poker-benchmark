using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.Models;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Lobby;

public class CreateModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public CreateModel(IBenchmarkApiClient api) => _api = api;

    public List<ModelInfo> AvailableModels { get; set; } = new();

    [BindProperty]
    public List<PlayerSlot> Players { get; set; } = new();

    [BindProperty]
    public int TotalRounds { get; set; } = 10;

    [BindProperty]
    public bool AdaptivePlay { get; set; } = true;

    [BindProperty]
    public bool RotateArchitect { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadModels();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadModels();

        // Remove empty/invalid slots
        Players = Players.Where(p =>
            (p.Type == "Human" && !string.IsNullOrWhiteSpace(p.Name)) ||
            (p.Type == "LLM" && !string.IsNullOrWhiteSpace(p.ModelId))
        ).ToList();

        if (Players.Count < 3)
        {
            ErrorMessage = "At least 3 players are required.";
            return Page();
        }

        if (!Players.Any(p => p.Type == "LLM"))
        {
            ErrorMessage = "At least 1 LLM player is required.";
            return Page();
        }

        var humanNames = Players.Where(p => p.Type == "Human").Select(p => p.Name!.Trim()).ToList();
        if (humanNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != humanNames.Count)
        {
            ErrorMessage = "Human player names must be unique.";
            return Page();
        }

        var request = new CreateInteractiveMatchRequest
        {
            TotalRounds = TotalRounds,
            Players = Players,
            RotateArchitect = RotateArchitect,
            AdaptivePlay = AdaptivePlay
        };

        var match = await _api.CreateInteractiveMatchAsync(request);
        if (match == null)
        {
            ErrorMessage = "Failed to create interactive match. Is the API running?";
            return Page();
        }

        // If there's exactly one human, redirect directly to Play with their playerId
        var firstHuman = Players.FirstOrDefault(p => p.Type == "Human");
        if (firstHuman != null)
        {
            var playerId = $"human:{firstHuman.Name!.Trim()}";
            return RedirectToPage("/Lobby/Play", new { id = match.Id, player = playerId });
        }

        return RedirectToPage("/Lobby/Play", new { id = match.Id });
    }

    private async Task LoadModels()
    {
        try
        {
            var modelList = await _api.GetModelsAsync();
            AvailableModels = modelList.Models;
        }
        catch
        {
            AvailableModels = new();
        }
    }
}
