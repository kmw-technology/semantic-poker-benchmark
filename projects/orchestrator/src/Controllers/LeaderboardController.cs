using Microsoft.AspNetCore.Mvc;
using SemanticPoker.Api.Infrastructure.Persistence;
using SemanticPoker.Shared.DTOs.Responses;

namespace SemanticPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly IMatchRepository _repository;

    public LeaderboardController(IMatchRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LeaderboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var entries = await _repository.GetLeaderboardAsync(ct);
        return Ok(new LeaderboardResponse { Entries = entries });
    }
}
