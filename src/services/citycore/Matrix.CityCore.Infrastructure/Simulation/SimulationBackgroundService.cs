using Matrix.CityCore.Application.UseCases.AdvanceSimulationTick;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Matrix.CityCore.Infrastructure.Simulation
{
    public sealed class SimulationBackgroundService(
        ISender sender,
        ILogger<SimulationBackgroundService> logger,
        SimulationLoopSettings settings) : BackgroundService
    {
        private readonly ILogger<SimulationBackgroundService> _logger = logger;
        private readonly ISender _sender = sender;
        private readonly SimulationLoopSettings _settings = settings;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                message: "City simulation loop started: Tick={TickMs}ms, SimMinutesPerTick={Minutes}",
                _settings.RealTimeTickMilliseconds,
                _settings.SimMinutesPerTick);

            var interval = TimeSpan.FromMilliseconds(_settings.RealTimeTickMilliseconds);
            using var timer = new PeriodicTimer(interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    try
                    {
                        await _sender.Send(
                            request: new AdvanceSimulationTickCommand(),
                            cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            exception: ex,
                            message: "Error advancing simulation tick");
                    }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("City simulation loop stopping.");
            }
        }
    }
}
