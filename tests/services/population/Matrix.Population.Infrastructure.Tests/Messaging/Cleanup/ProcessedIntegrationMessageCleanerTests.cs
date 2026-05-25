using Matrix.Population.Infrastructure.Messaging.Cleanup;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Messaging.Cleanup
{
    public sealed class ProcessedIntegrationMessageCleanerTests
    {
        [Fact]
        public async Task DeleteBatchAsync_DeletesOldestEligibleMarkersUpToBatchSize()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            dbContext.ProcessedIntegrationMessages.AddRange(
                new ProcessedIntegrationMessage(
                    consumer: "A",
                    messageId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 1,
                        hour: 1,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                new ProcessedIntegrationMessage(
                    consumer: "B",
                    messageId: Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 1,
                        hour: 2,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                new ProcessedIntegrationMessage(
                    consumer: "C",
                    messageId: Guid.Parse("30000000-0000-0000-0000-000000000003"),
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 1,
                        hour: 3,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                new ProcessedIntegrationMessage(
                    consumer: "D",
                    messageId: Guid.Parse("40000000-0000-0000-0000-000000000004"),
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 2,
                        hour: 1,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            await dbContext.SaveChangesAsync();
            var cleaner = new ProcessedIntegrationMessageCleaner(dbContext);

            int deletedCount = await cleaner.DeleteBatchAsync(
                processedBeforeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 23,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                batchSize: 2,
                cancellationToken: CancellationToken.None);

            var remainingIds = (await dbContext.ProcessedIntegrationMessages.ToListAsync())
               .OrderBy(x => x.ProcessedAtUtc)
               .Select(x => x.MessageId)
               .ToList();

            Assert.Equal(
                expected: 2,
                actual: deletedCount);
            Assert.Equal(
                expected:
                [
                    Guid.Parse("30000000-0000-0000-0000-000000000003"),
                    Guid.Parse("40000000-0000-0000-0000-000000000004")
                ],
                actual: remainingIds);
        }

        [Fact]
        public async Task DeleteBatchAsync_WhenNoEligibleMarkersExist_ReturnsZero()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            dbContext.ProcessedIntegrationMessages.Add(
                new ProcessedIntegrationMessage(
                    consumer: "Fresh",
                    messageId: Guid.Parse("a5b1866e-422c-4a3b-a9d1-a4a6a6d63c7c"),
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 2,
                        hour: 1,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            await dbContext.SaveChangesAsync();
            var cleaner = new ProcessedIntegrationMessageCleaner(dbContext);

            int deletedCount = await cleaner.DeleteBatchAsync(
                processedBeforeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 23,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                batchSize: 5,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: deletedCount);
            Assert.Equal(
                expected: 1,
                actual: await dbContext.ProcessedIntegrationMessages.CountAsync());
        }
    }
}
