using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityEconomyProgressionStateRepository
    {
        Task<CityEconomyProgressionState?> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityEconomyProgressionState state,
            CancellationToken cancellationToken = default);
    }
}
