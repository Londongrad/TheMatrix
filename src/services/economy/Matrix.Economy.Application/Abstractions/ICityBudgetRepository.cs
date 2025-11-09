using Matrix.Economy.Domain.Aggregates;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityBudgetRepository
    {
        Task<CityBudget?> GetCurrentAsync(CancellationToken cancellationToken = default);
        void Add(CityBudget cityBudget);
    }
}
