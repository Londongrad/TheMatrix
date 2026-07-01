using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Facilities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories
{
    public sealed class CareFacilityRepository(HealthcareDbContext dbContext)
        : ICareFacilityRepository
    {
        public async Task<IReadOnlyList<CareFacility>> GetByIdsAsync(
            IReadOnlyCollection<CareFacilityId> facilityIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(facilityIds);
            if (facilityIds.Count == 0)
                return [];

            CareFacilityId[] ids = facilityIds.Distinct().ToArray();
            return await dbContext.CareFacilities
               .Where(facility => ids.Contains(facility.Id))
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<CareFacility> facilities,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(facilities);
            return dbContext.CareFacilities.AddRangeAsync(
                entities: facilities,
                cancellationToken: cancellationToken);
        }
    }
}
