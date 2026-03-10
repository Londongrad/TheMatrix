using Matrix.Economy.Domain.Aggregates;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityBudgetRepository
    {
        Task<CityBudget?> GetByCityAsync(Guid cityId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CityBudget>> ListAsync(CancellationToken cancellationToken = default);
        void Add(CityBudget cityBudget);
    }
}
