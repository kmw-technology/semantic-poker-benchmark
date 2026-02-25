using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.Interfaces;

public interface IMatchRunner
{
    Task<Match> StartMatchAsync(MatchConfig config, CancellationToken ct = default);
    Task<Match> GetMatchStatusAsync(Guid matchId, CancellationToken ct = default);
    Task PauseMatchAsync(Guid matchId, CancellationToken ct = default);
    Task ResumeMatchAsync(Guid matchId, CancellationToken ct = default);
    Task CancelMatchAsync(Guid matchId, CancellationToken ct = default);
}
