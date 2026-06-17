using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
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
