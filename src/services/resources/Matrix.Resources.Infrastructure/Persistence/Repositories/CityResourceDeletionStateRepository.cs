using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Persistence.Repositories
{
    public sealed class CityResourceDeletionStateRepository(ResourcesDbContext dbContext)
        : ICityResourceDeletionStateRepository
    {
        public async Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            CityResourceDeletionState? state = await dbContext.CityResourceDeletionStates.FindAsync(
                keyValues: [cityId],
                cancellationToken: cancellationToken);

            return state?.DeletedAtUtc;
        }

        public async Task RecordAsync(
            Guid cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken)
        {
            CityResourceDeletionState? state = await dbContext.CityResourceDeletionStates.FindAsync(
                keyValues: [cityId],
                cancellationToken: cancellationToken);

            if (state is null)
            {
                await dbContext.CityResourceDeletionStates.AddAsync(
                    entity: new CityResourceDeletionState(
                        cityId: cityId,
                        deletedAtUtc: deletedAtUtc,
                        updatedAtUtc: updatedAtUtc),
                    cancellationToken: cancellationToken);
                return;
            }

            state.Record(
                deletedAtUtc: deletedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }
    }
}
