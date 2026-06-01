using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Abstractions.Outbox;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Outbox
{
    public interface IClassicCityOutboxWriter : ISimulationCoreOutboxWriter
    {
        Task AddCityEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);

        Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);
    }
}
