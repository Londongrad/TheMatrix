using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation
{
    public sealed class ClassicCitySimulationAdvanceHandler(
        IWeatherAdvanceExecutor weatherAdvanceExecutor,
        ISimulationCoreOutboxWriter outboxWriter) : ISimulationScenarioAdvanceHandler
    {
        public SimulationHostKind HostKind => SimulationHostKind.City;

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

            await outboxWriter.AddCityTimeAdvancedAsync(
                cityId: cityId,
                simulationId: host.SimulationId,
                simulationKind: host.SimulationKind,
                from: advancedEvent.From,
                to: advancedEvent.To,
                tickId: advancedEvent.TickId,
                speed: advancedEvent.Speed,
                phase: CityTickPhaseV1.AdvanceTime,
                cancellationToken: cancellationToken);

            if (cityWeather is not null && cityWeather.DomainEvents.Count > 0)
            {
                await outboxWriter.AddWeatherEventsAsync(
                    domainEvents: cityWeather.DomainEvents,
                    cancellationToken: cancellationToken);
                cityWeather.ClearDomainEvents();
            }

            foreach (CityTickPhaseV1 phase in GetClassicCityPhaseWatermarks())
            {
                await outboxWriter.AddCityTickPhaseReachedAsync(
                    cityId: cityId,
                    simulationId: host.SimulationId,
                    simulationKind: host.SimulationKind,
                    from: advancedEvent.From,
                    to: advancedEvent.To,
                    tickId: advancedEvent.TickId,
                    speed: advancedEvent.Speed,
                    phase: phase,
                    cancellationToken: cancellationToken);
            }
        }

        private static IReadOnlyList<CityTickPhaseV1> GetClassicCityPhaseWatermarks()
        {
            return
            [
                CityTickPhaseV1.SystemsDegradation,
                CityTickPhaseV1.IncidentGeneration,
                CityTickPhaseV1.DispatchExecution,
                CityTickPhaseV1.ResourceSettlement,
                CityTickPhaseV1.BudgetSettlement,
                CityTickPhaseV1.PopulationReaction,
                CityTickPhaseV1.Projection,
                CityTickPhaseV1.TickCompleted
            ];
        }
    }
}
