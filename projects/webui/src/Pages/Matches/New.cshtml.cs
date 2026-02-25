using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.Models;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages.Matches;

public class NewModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public NewModel(IBenchmarkApiClient api)
    {
        _api = api;
    }

    public List<ModelInfo> AvailableModels { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public List<string> SelectedModels { get; set; } = new();

    [BindProperty]
    public int TotalRounds { get; set; } = 10;

    [BindProperty]
    public bool AdaptivePlay { get; set; } = true;

    [BindProperty]
    public bool RotateArchitect { get; set; } = true;

    [BindProperty]
    public int? RandomSeed { get; set; }

    public async Task OnGetAsync()
    {
        var modelList = await _api.GetModelsAsync();
        AvailableModels = modelList.Models;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (SelectedModels.Count < 3)
        {
            ErrorMessage = "Please select at least 3 models.";
            var modelList = await _api.GetModelsAsync();
            AvailableModels = modelList.Models;
            return Page();
        }

        var request = new CreateMatchRequest
        {
            TotalRounds = TotalRounds,
            ModelIds = SelectedModels,
            RotateArchitect = RotateArchitect,
            AdaptivePlay = AdaptivePlay,
            RandomSeed = RandomSeed
        };

        var match = await _api.CreateMatchAsync(request);
        return RedirectToPage("/Matches/Detail", new { id = match.Id });
    }
}
