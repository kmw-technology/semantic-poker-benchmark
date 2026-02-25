using Microsoft.AspNetCore.Mvc;
using SemanticPoker.Api.Infrastructure.Persistence;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Interfaces;

namespace SemanticPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILlmAdapter _llmAdapter;
    private readonly BenchmarkDbContext _db;

    public HealthController(ILlmAdapter llmAdapter, BenchmarkDbContext db)
    {
        _llmAdapter = llmAdapter;
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var ollamaHealthy = await _llmAdapter.IsHealthyAsync(ct);
        var models = ollamaHealthy ? await _llmAdapter.ListAvailableModelsAsync(ct) : new();

        var dbStatus = "Unknown";
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(ct);
            dbStatus = canConnect ? "Connected" : "Disconnected";
        }
        catch (Exception ex)
        {
            dbStatus = $"Error: {ex.Message}";
        }

        var response = new HealthCheckResponse
        {
            IsHealthy = ollamaHealthy && dbStatus == "Connected",
            OllamaConnected = ollamaHealthy,
            AvailableModels = models.Count,
            DatabaseStatus = dbStatus
        };

        return Ok(response);
    }
}
