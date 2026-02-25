using Microsoft.AspNetCore.Mvc;
using SemanticPoker.Shared.DTOs.Requests;
using SemanticPoker.Shared.DTOs.Responses;
using SemanticPoker.Shared.Interfaces;

namespace SemanticPoker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IStateGenerator _stateGenerator;
    private readonly ISentenceEngine _sentenceEngine;
    private readonly ILlmAdapter _llmAdapter;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        IStateGenerator stateGenerator,
        ISentenceEngine sentenceEngine,
        ILlmAdapter llmAdapter,
        ILogger<DebugController> logger)
    {
        _stateGenerator = stateGenerator;
        _sentenceEngine = sentenceEngine;
        _llmAdapter = llmAdapter;
        _logger = logger;
    }

    [HttpPost("generate-state")]
    [ProducesResponseType(typeof(GenerateStateResponse), StatusCodes.Status200OK)]
    public IActionResult GenerateState([FromBody] GenerateStateRequest request)
    {
        var state = _stateGenerator.Generate(request.Seed);

        var response = new GenerateStateResponse
        {
            Doors = state.Doors.Select(d => new DoorDto
            {
                Label = d.Label,
                Type = d.Type.ToString()
            }).ToList(),
            TreasureDoor = state.TreasureDoor.Label,
            TrapDoor = state.TrapDoor.Label,
            Seed = state.Seed
        };

        return Ok(response);
    }

    [HttpPost("generate-sentences")]
    [ProducesResponseType(typeof(GenerateSentencesResponse), StatusCodes.Status200OK)]
    public IActionResult GenerateSentences([FromBody] GenerateSentencesRequest request)
    {
        var state = _stateGenerator.Generate(request.Seed);

        var sentences = _sentenceEngine.GenerateTrueSentences(state, request.Count);

        var response = new GenerateSentencesResponse
        {
            State = new GenerateStateResponse
            {
                Doors = state.Doors.Select(d => new DoorDto
                {
                    Label = d.Label,
                    Type = d.Type.ToString()
                }).ToList(),
                TreasureDoor = state.TreasureDoor.Label,
                TrapDoor = state.TrapDoor.Label,
                Seed = state.Seed
            },
            Sentences = sentences.Select((s, i) => new SentenceDto
            {
                Index = i,
                Text = s.Text,
                Source = s.Source,
                IsTruthful = s.IsTruthful
            }).ToList()
        };

        return Ok(response);
    }

    [HttpPost("test-prompt")]
    [ProducesResponseType(typeof(TestPromptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestPrompt([FromBody] TestPromptRequest request, CancellationToken ct)
    {
        try
        {
            var options = new LlmRequestOptions
            {
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens
            };

            var llmResponse = await _llmAdapter.SendPromptAsync(
                request.ModelId, request.SystemPrompt, request.UserPrompt, options, ct);

            return Ok(new TestPromptResponse
            {
                Response = llmResponse.Content,
                PromptTokens = llmResponse.PromptTokens,
                CompletionTokens = llmResponse.CompletionTokens,
                ResponseTimeMs = llmResponse.ResponseTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test prompt failed for model {ModelId}", request.ModelId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Prompt test failed",
                Details = ex.Message
            });
        }
    }
}
