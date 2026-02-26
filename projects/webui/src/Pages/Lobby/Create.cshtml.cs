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
    public List<string> SelectedModels { get; set; } = new();

    [BindProperty]
    public string HumanPlayerName { get; set; } = "Human";

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
        if (SelectedModels.Count < 2)
        {
            ErrorMessage = "Please select at least 2 LLM opponents.";
            await LoadModels();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(HumanPlayerName))
        {
            ErrorMessage = "Please enter your player name.";
            await LoadModels();
            return Page();
        }

        var request = new CreateInteractiveMatchRequest
        {
            TotalRounds = TotalRounds,
            ModelIds = SelectedModels,
            HumanPlayerName = HumanPlayerName.Trim(),
            RotateArchitect = RotateArchitect,
            AdaptivePlay = AdaptivePlay
        };

        var match = await _api.CreateInteractiveMatchAsync(request);
        if (match == null)
        {
            ErrorMessage = "Failed to create interactive match. Is the API running?";
            await LoadModels();
            return Page();
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
