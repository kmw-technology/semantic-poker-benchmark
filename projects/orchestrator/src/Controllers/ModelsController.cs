using Microsoft.AspNetCore.Mvc;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Interfaces;

namespace SemanticPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly ILlmAdapter _llmAdapter;

    public ModelsController(ILlmAdapter llmAdapter)
    {
        _llmAdapter = llmAdapter;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ModelListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var models = await _llmAdapter.ListAvailableModelsAsync(ct);
        return Ok(new ModelListResponse { Models = models });
    }
}
