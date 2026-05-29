using Matrix.BuildingBlocks.Domain.Events;
using Matrix.Simulation.Primitives;
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

        Task AddSimulationTickPhaseReachedAsync(
            SimulationHost host,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            SimulationPhaseKey phaseKey,
            CancellationToken cancellationToken);

        Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);
    }
}
