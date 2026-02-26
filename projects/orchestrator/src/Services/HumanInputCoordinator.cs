using System.Collections.Concurrent;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.DTOs.Responses;

namespace SemanticPoker.Api.Services;

public class HumanInputCoordinator
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<HumanInput>> _pendingInputs = new();
    private readonly ConcurrentDictionary<Guid, HumanInputRequest> _waitingContext = new();

    /// <summary>
    /// Called by MatchRunnerService when it needs human input. Blocks until human submits.
    /// </summary>
    public async Task<HumanInput> WaitForHumanInputAsync(
        Guid matchId, HumanInputRequest request, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<HumanInput>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingInputs[matchId] = tcs;
        _waitingContext[matchId] = request;

        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        try
        {
            return await tcs.Task;
        }
        finally
        {
            _pendingInputs.TryRemove(matchId, out _);
            _waitingContext.TryRemove(matchId, out _);
        }
    }

    /// <summary>
    /// Called by Controller when human submits input via HTTP.
    /// </summary>
    public bool SubmitInput(Guid matchId, HumanInput input)
    {
        if (_pendingInputs.TryRemove(matchId, out var tcs))
        {
            return tcs.TrySetResult(input);
        }
        return false;
    }

    /// <summary>
    /// Called by Controller/UI to get what the game is waiting for.
    /// </summary>
    public HumanInputRequest? GetWaitingContext(Guid matchId)
    {
        _waitingContext.TryGetValue(matchId, out var ctx);
        return ctx;
    }

    /// <summary>
    /// Cleanup on match cancellation.
    /// </summary>
    public void CancelMatch(Guid matchId)
    {
        if (_pendingInputs.TryRemove(matchId, out var tcs))
        {
            tcs.TrySetCanceled();
        }
        _waitingContext.TryRemove(matchId, out _);
    }
}

public class HumanInputRequest
{
    public PlayerRole InputType { get; set; }
    public int RoundNumber { get; set; }
    public PlayerRole HumanRole { get; set; }
    public char? TreasureDoor { get; set; }
    public char? TrapDoor { get; set; }
    public List<string>? EngineSentences { get; set; }
    public List<ShuffledSentenceDto>? ShuffledSentences { get; set; }
}

public class HumanInput
{
    public PlayerRole InputType { get; set; }
    public List<string>? ArchitectSentences { get; set; }
    public char? ChosenDoor { get; set; }
    public string? Reasoning { get; set; }
}
