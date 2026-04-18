using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence
{
    public interface ICityRepository
    {
        Task<City?> GetByIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task<City?> GetByProvisioningCorrelationIdAsync(
            Guid provisioningCorrelationId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<City>> ListAsync(
            bool includeArchived,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<City>> ListProvisioningAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<City>> ListRecoverableProvisioningAsync(
            DateTimeOffset asOfUtc,
            int limit,
            CancellationToken cancellationToken);

        Task AddAsync(
            City city,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            City city,
            CancellationToken cancellationToken);
    }
}
