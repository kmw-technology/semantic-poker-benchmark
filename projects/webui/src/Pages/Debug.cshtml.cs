using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Models;
using SemanticPoker.WebUI.Services;

namespace SemanticPoker.WebUI.Pages;

public class DebugModel : PageModel
{
    private readonly IBenchmarkApiClient _api;

    public DebugModel(IBenchmarkApiClient api)
    {
        _api = api;
    }

    public List<ModelInfo> AvailableModels { get; set; } = new();

    // State Generation
    [BindProperty]
    public int? StateSeed { get; set; }
    public GenerateStateResponse? GeneratedState { get; set; }

    // Sentence Generation
    [BindProperty]
    public int? SentenceSeed { get; set; }
    [BindProperty]
    public int SentenceCount { get; set; } = 2;
    public GenerateSentencesResponse? GeneratedSentences { get; set; }

    // Prompt Testing
    [BindProperty]
    public string PromptModelId { get; set; } = string.Empty;
    [BindProperty]
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";
    [BindProperty]
    public string UserPrompt { get; set; } = string.Empty;
    [BindProperty]
    public double PromptTemperature { get; set; } = 0.7;
    [BindProperty]
    public int PromptMaxTokens { get; set; } = 1024;
    public TestPromptResponse? PromptResult { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var modelList = await _api.GetModelsAsync();
        AvailableModels = modelList.Models;
    }

    public async Task<IActionResult> OnPostGenerateStateAsync()
    {
        await LoadModels();
        try
        {
            GeneratedState = await _api.GenerateStateAsync(new GenerateStateRequest { Seed = StateSeed });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"State generation failed: {ex.Message}";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateSentencesAsync()
    {
        await LoadModels();
        try
        {
            GeneratedSentences = await _api.GenerateSentencesAsync(new GenerateSentencesRequest
            {
                Seed = SentenceSeed,
                Count = SentenceCount
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Sentence generation failed: {ex.Message}";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostTestPromptAsync()
    {
        await LoadModels();
        try
        {
            PromptResult = await _api.TestPromptAsync(new TestPromptRequest
            {
                ModelId = PromptModelId,
                SystemPrompt = SystemPrompt,
                UserPrompt = UserPrompt,
                Temperature = PromptTemperature,
                MaxTokens = PromptMaxTokens
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Prompt test failed: {ex.Message}";
        }
        return Page();
    }

    private async Task LoadModels()
    {
        var modelList = await _api.GetModelsAsync();
        AvailableModels = modelList.Models;
    }
}
