using Matrix.BuildingBlocks.Application.Events;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation
{
    public sealed class ClassicCitySimulationAdvanceHandler(
        IWeatherAdvanceExecutor weatherAdvanceExecutor,
        ICityActiveTripAdvanceExecutor activeTripAdvanceExecutor,
        IClassicCityOutboxWriter outboxWriter) : ISimulationScenarioAdvanceHandler
    {
        public SimulationRuntimeKey RuntimeKey => ClassicCityRuntime.Key;

        public async Task HandleAdvancedAsync(
            SimulationHost host,
            SimulationTimeAdvancedDomainEvent advancedEvent,
            CancellationToken cancellationToken)
        {
            CityId cityId = new(host.HostId.Value);

            CityWeather? cityWeather = await weatherAdvanceExecutor.AdvanceAsync(
                cityId: cityId,
                evaluatedAt: advancedEvent.To,
                cancellationToken: cancellationToken);

            await activeTripAdvanceExecutor.AdvanceAsync(
                cityId: cityId,
                fromSimTimeUtc: advancedEvent.From.ValueUtc,
                toSimTimeUtc: advancedEvent.To.ValueUtc,
                tickId: advancedEvent.TickId.Value,
                cancellationToken: cancellationToken);

            await AddSimulationPhaseAsync(
                host: host,
                advancedEvent: advancedEvent,
                phase: CityTickPhase.AdvanceTime,
                cancellationToken: cancellationToken);

            if (cityWeather is not null && cityWeather.DomainEvents.Count > 0)
                await DomainEventDispatchHelper.PublishAndClearAsync(
                    source: cityWeather,
                    publish: outboxWriter.AddWeatherEventsAsync,
                    cancellationToken: cancellationToken);

            foreach (CityTickPhase phase in GetClassicCityPhaseWatermarks())
            {
                await AddSimulationPhaseAsync(
                    host: host,
                    advancedEvent: advancedEvent,
                    phase: phase,
                    cancellationToken: cancellationToken);
            }
        }

        private Task AddSimulationPhaseAsync(
            SimulationHost host,
            SimulationTimeAdvancedDomainEvent advancedEvent,
            CityTickPhase phase,
            CancellationToken cancellationToken)
        {
            return outboxWriter.AddSimulationTickPhaseReachedAsync(
                host: host,
                from: advancedEvent.From,
                to: advancedEvent.To,
                tickId: advancedEvent.TickId,
                speed: advancedEvent.Speed,
                phaseKey: phase.ToPhaseKey(),
                cancellationToken: cancellationToken);
        }

        private static IReadOnlyList<CityTickPhase> GetClassicCityPhaseWatermarks()
        {
            return
            [
                CityTickPhase.SystemsDegradation,
                CityTickPhase.IncidentGeneration,
                CityTickPhase.DispatchExecution,
                CityTickPhase.ResourceSettlement,
                CityTickPhase.BudgetSettlement,
                CityTickPhase.PopulationReaction,
                CityTickPhase.Projection,
                CityTickPhase.TickCompleted
            ];
        }
    }
}
