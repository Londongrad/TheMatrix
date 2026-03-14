using Matrix.Economy.Domain.Aggregates;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityBusinessRepository
    {
        Task<CityBusiness?> GetByIdAsync(Guid businessId, CancellationToken cancellationToken = default);
        Task<CityBusiness?> GetByCityAndExternalReferenceCodeAsync(Guid cityId, string externalReferenceCode, CancellationToken cancellationToken = default);
        Task<CityBusiness?> GetByCityAndTemplateKeyAsync(Guid cityId, string templateKey, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CityBusiness>> ListByCityAsync(Guid cityId, CancellationToken cancellationToken = default);
        void Add(CityBusiness cityBusiness);
    }
}
