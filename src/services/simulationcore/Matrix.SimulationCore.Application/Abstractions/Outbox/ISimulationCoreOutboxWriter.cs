using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Abstractions.Outbox
{
    public interface ISimulationCoreOutboxWriter
    {
        Task AddCityEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);

        Task AddCityTimeAdvancedAsync(
            CityId cityId,
            SimulationId simulationId,
            SimulationKind simulationKind,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CityTickPhaseV1 phase,
            CancellationToken cancellationToken);

        Task AddCityTickPhaseReachedAsync(
            CityId cityId,
            SimulationId simulationId,
            SimulationKind simulationKind,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CityTickPhaseV1 phase,
            CancellationToken cancellationToken);

        Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);
    }
}
