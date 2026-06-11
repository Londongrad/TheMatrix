using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CitySystemsDeletionStateRepository(SimulationSystemsDbContext dbContext)
        : ICitySystemsDeletionStateRepository
    {
        public async Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            CitySystemsDeletionState? state = await dbContext.CitySystemsDeletionStates.FindAsync(
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
            CitySystemsDeletionState? state = await dbContext.CitySystemsDeletionStates.FindAsync(
                keyValues: [cityId],
                cancellationToken: cancellationToken);

            if (state is null)
            {
                await dbContext.CitySystemsDeletionStates.AddAsync(
                    entity: new CitySystemsDeletionState(
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
