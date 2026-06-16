using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityBudgetRepository
    {
        Task<CityBudget?> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityBudget>> ListAsync(CancellationToken cancellationToken = default);
        void Add(CityBudget cityBudget);
    }
}
