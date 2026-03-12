using Matrix.Economy.Domain.Aggregates;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityHouseholdObligationRepository
    {
        Task<CityHouseholdObligation?> GetByIdAsync(Guid obligationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CityHouseholdObligation>> ListByCityAsync(Guid cityId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CityHouseholdObligation>> ListByHouseholdAsync(Guid householdAccountId, CancellationToken cancellationToken = default);
        void Add(CityHouseholdObligation obligation);
    }
}
