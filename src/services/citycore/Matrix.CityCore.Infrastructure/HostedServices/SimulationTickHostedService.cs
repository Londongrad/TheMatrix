using System.Diagnostics;
using Matrix.CityCore.Application.UseCases.Simulation.AdvanceRunningSimulations;
using Matrix.CityCore.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.CityCore.Infrastructure.HostedServices
{
    public sealed class SimulationTickHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SimulationTickOptions> options,
        ILogger<SimulationTickHostedService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            SimulationTickOptions tickOptions = options.Value;

            if (tickOptions.PeriodMilliseconds <= 0)
                throw new InvalidOperationException("CityCore:Tick:PeriodMilliseconds must be > 0.");

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickOptions.PeriodMilliseconds));
            var stopwatch = Stopwatch.StartNew();
            TimeSpan lastElapsed = stopwatch.Elapsed;

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                TimeSpan currentElapsed = stopwatch.Elapsed;
                TimeSpan realDelta = currentElapsed - lastElapsed;
                lastElapsed = currentElapsed;

                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    AdvanceRunningSimulationsResult result = await mediator.Send(
                        request: new AdvanceRunningSimulationsCommand(realDelta),
                        cancellationToken: cancellationToken);

                    logger.LogDebug(
                        message:
                        "CityCore tick processed {ProcessedCount} cities. Advanced: {AdvancedCount}, skipped: {SkippedCount}, failed: {FailedCount}.",
                        result.ProcessedCount,
                        result.AdvancedCount,
                        result.SkippedCount,
                        result.FailedCount);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        exception: ex,
                        message: "CityCore tick loop iteration failed.");
                }
            }
        }
    }
}
