using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Abstractions.Outbox
{
    public interface ISimulationCoreOutboxWriter
    {
        Task AddSimulationEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);

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
            CityTickPhase phase,
            CancellationToken cancellationToken);

        Task AddCityTickPhaseReachedAsync(
            CityId cityId,
            SimulationId simulationId,
            SimulationKind simulationKind,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CityTickPhase phase,
            CancellationToken cancellationToken);

        Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);
    }
}
