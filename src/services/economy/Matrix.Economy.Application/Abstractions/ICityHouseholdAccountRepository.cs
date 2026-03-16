using Matrix.Economy.Domain.Aggregates;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityHouseholdAccountRepository
    {
        Task<CityHouseholdAccount?> GetByIdAsync(
            Guid householdAccountId,
            CancellationToken cancellationToken = default);

        Task<CityHouseholdAccount?> GetByCityAndExternalReferenceCodeAsync(
            Guid cityId,
            string externalReferenceCode,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityHouseholdAccount>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        void Add(CityHouseholdAccount householdAccount);
    }
}
