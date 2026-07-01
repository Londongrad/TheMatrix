using Matrix.Healthcare.Domain.Facilities;

namespace Matrix.Healthcare.Application.Abstractions
{
    public interface ICareFacilityRepository
    {
        Task<IReadOnlyList<CareFacility>> GetByIdsAsync(
            IReadOnlyCollection<CareFacilityId> facilityIds,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<CareFacility> facilities,
            CancellationToken cancellationToken = default);
    }
}
