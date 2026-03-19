using Matrix.Economy.Domain.Entities;

namespace Matrix.Economy.Application.Abstractions
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
