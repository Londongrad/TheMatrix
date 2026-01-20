using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.Abstractions.Persistence
{
    public interface ICityRepository
    {
        Task<City?> GetByIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<City>> ListAsync(
            bool includeArchived,
            CancellationToken cancellationToken);

        Task AddAsync(
            City city,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            City city,
            CancellationToken cancellationToken);
    }
}
