using Matrix.BuildingBlocks.Domain.Events;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Outbox
{
    public interface IClassicCityOutboxWriter
    {
        Task AddCityEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);

        Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);
    }
}
