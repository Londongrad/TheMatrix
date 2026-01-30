using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Abstractions
{
    public interface IHouseholdWriteRepository
    {
        Task DeleteAllAsync(CancellationToken cancellationToken = default);

        Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<Household> households,
            CancellationToken cancellationToken = default);
    }
}