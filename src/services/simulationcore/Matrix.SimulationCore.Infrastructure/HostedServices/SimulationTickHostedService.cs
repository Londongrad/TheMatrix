using System.Diagnostics;
using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations;
using Matrix.SimulationCore.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.SimulationCore.Infrastructure.HostedServices
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
                throw new InvalidOperationException("SimulationCore:Tick:PeriodMilliseconds must be > 0.");

            if (tickOptions.FixedStepSeconds <= 0)
                throw new InvalidOperationException("SimulationCore:Tick:FixedStepSeconds must be > 0.");

            if (tickOptions.MaxStepsPerSimulationPerCycle <= 0)
                throw new InvalidOperationException("SimulationCore:Tick:MaxStepsPerSimulationPerCycle must be > 0.");

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickOptions.PeriodMilliseconds));
            var stopwatch = Stopwatch.StartNew();
            TimeSpan lastElapsed = stopwatch.Elapsed;

            try
            {
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
                            "SimulationCore tick processed {ProcessedCount} cities. Advanced: {AdvancedCount}, no step due: {NoStepDueCount}, lagging: {LaggingCount}, failed: {FailedCount}, fixed steps: {TotalStepsProcessed}.",
                            result.ProcessedCount,
                            result.AdvancedCount,
                            result.NoStepDueCount,
                            result.LaggingCount,
                            result.FailedCount,
                            result.TotalStepsProcessed);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            exception: ex,
                            message: "SimulationCore tick loop iteration failed.");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("SimulationCore tick loop stopped.");
            }
        }
    }
}
