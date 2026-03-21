using Matrix.ApiGateway.Configurations.Options;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionRecoveryHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ClassicCitySetupSessionOptions> options,
        ILogger<ClassicCitySetupSessionRecoveryHostedService> logger)
        : BackgroundService
    {
        private readonly ClassicCitySetupSessionOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.ReconciliationEnabled)
            {
                logger.LogInformation("Classic City setup session reconciliation is disabled by configuration.");
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.ReconciliationIntervalSeconds));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await ReconcileOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
        }

        private async Task ReconcileOnceAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IClassicCitySetupSessionStore store =
                scope.ServiceProvider.GetRequiredService<IClassicCitySetupSessionStore>();
            IClassicCitySetupSessionService service =
                scope.ServiceProvider.GetRequiredService<IClassicCitySetupSessionService>();

            IReadOnlyList<Guid> sessionIds = await store.ListTrackedSessionIdsAsync(cancellationToken);

            foreach (Guid sessionId in sessionIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await service.ReconcileAsync(
                        sessionId: sessionId,
                        cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        exception: ex,
                        message: "Classic City setup session reconciliation failed for sessionId={SessionId}.",
                        sessionId);
                }
            }
        }
    }
}
