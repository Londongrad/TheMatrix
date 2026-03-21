using Matrix.BuildingBlocks.Domain.Events;
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
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CancellationToken cancellationToken);

        Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);
    }
}
