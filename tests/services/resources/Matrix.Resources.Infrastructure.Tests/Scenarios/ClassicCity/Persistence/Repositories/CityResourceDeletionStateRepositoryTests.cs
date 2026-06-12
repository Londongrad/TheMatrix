using Matrix.Resources.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityResourceDeletionStateRepositoryTests
    {
        [Fact]
        public async Task RecordAsync_PersistsLatestDeletionTimestamp()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var repository = new CityResourceDeletionStateRepository(dbContext);

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
