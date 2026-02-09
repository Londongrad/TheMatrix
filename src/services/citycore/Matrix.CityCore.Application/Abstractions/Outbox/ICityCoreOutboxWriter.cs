using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Abstractions.Outbox
{
    public interface ICityCoreOutboxWriter
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
