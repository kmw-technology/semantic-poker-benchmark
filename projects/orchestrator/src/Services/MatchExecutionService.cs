using SemanticPoker.Api.Infrastructure.Persistence;

namespace SemanticPoker.Api.Services;

public class MatchExecutionService : BackgroundService
{
    private readonly MatchExecutionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchExecutionService> _logger;

    public MatchExecutionService(
        MatchExecutionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<MatchExecutionService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Match execution service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var matchId = await _queue.DequeueAsync(stoppingToken);
                _logger.LogInformation("Dequeued match {MatchId} for execution", matchId);

                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<MatchRunnerService>();
                await runner.RunMatchAsync(matchId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing match from queue");
            }
        }

        _logger.LogInformation("Match execution service stopped");
    }
}
