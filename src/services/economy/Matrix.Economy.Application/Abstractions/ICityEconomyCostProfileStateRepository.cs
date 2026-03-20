using Matrix.Economy.Domain.Entities;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityEconomyCostProfileStateRepository
    {
        Task<CityEconomyCostProfileState?> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityEconomyCostProfileState state,
            CancellationToken cancellationToken = default);
    }
}
