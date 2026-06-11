using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CitySystemsDeletionStateRepositoryTests
    {
        [Fact]
        public async Task RecordAsync_PersistsLatestDeletionTimestamp()
        {
            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            var repository = new CitySystemsDeletionStateRepository(dbContext);

            await repository.RecordAsync(
                cityId: CityId,
                deletedAtUtc: LaterUtc,
                updatedAtUtc: LaterUtc.AddMinutes(1),
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            await repository.RecordAsync(
                cityId: CityId,
                deletedAtUtc: LaterUtc.AddMinutes(5),
                updatedAtUtc: LaterUtc.AddMinutes(6),
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            Assert.Equal(
                expected: LaterUtc.AddMinutes(5),
                actual: await repository.GetDeletedAtUtcAsync(
                    cityId: CityId,
                    cancellationToken: CancellationToken.None));
        }
    }
}
