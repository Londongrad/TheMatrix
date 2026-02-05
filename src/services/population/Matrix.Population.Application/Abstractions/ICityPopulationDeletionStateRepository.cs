using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Abstractions
{
    public interface ICityPopulationDeletionStateRepository
    {
        Task<CityPopulationDeletionState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityPopulationDeletionState state,
            CancellationToken cancellationToken = default);
    }
}
