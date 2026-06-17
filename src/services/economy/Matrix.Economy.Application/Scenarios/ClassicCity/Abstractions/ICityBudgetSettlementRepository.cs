using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityBudgetSettlementRepository
    {
        Task<bool> ExistsAsync(
            Guid cityId,
            long tickId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            CityBudgetSettlement settlement,
            CancellationToken cancellationToken = default);
    }
}
