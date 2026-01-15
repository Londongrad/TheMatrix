using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Application.Abstractions.Outbox
{
    public interface ICityCoreOutboxWriter
    {
        Task AddCityTimeAdvancedAsync(
            CityId cityId,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CancellationToken cancellationToken);
    }
}
