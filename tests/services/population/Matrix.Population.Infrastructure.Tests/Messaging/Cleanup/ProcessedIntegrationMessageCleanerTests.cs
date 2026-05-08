using Matrix.Population.Infrastructure.Messaging.Cleanup;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Messaging.Cleanup;

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
                processedAtUtc: new DateTimeOffset(2048, 5, 1, 1, 0, 0, TimeSpan.Zero)),
            new ProcessedIntegrationMessage(
                consumer: "B",
                messageId: Guid.Parse("20000000-0000-0000-0000-000000000002"),
                processedAtUtc: new DateTimeOffset(2048, 5, 1, 2, 0, 0, TimeSpan.Zero)),
            new ProcessedIntegrationMessage(
                consumer: "C",
                messageId: Guid.Parse("30000000-0000-0000-0000-000000000003"),
                processedAtUtc: new DateTimeOffset(2048, 5, 1, 3, 0, 0, TimeSpan.Zero)),
            new ProcessedIntegrationMessage(
                consumer: "D",
                messageId: Guid.Parse("40000000-0000-0000-0000-000000000004"),
                processedAtUtc: new DateTimeOffset(2048, 5, 2, 1, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();
        var cleaner = new ProcessedIntegrationMessageCleaner(dbContext);

        int deletedCount = await cleaner.DeleteBatchAsync(
            processedBeforeUtc: new DateTimeOffset(2048, 5, 1, 23, 0, 0, TimeSpan.Zero),
            batchSize: 2,
            cancellationToken: CancellationToken.None);

        List<Guid> remainingIds = (await dbContext.ProcessedIntegrationMessages.ToListAsync())
           .OrderBy(x => x.ProcessedAtUtc)
           .Select(x => x.MessageId)
           .ToList();

        Assert.Equal(2, deletedCount);
        Assert.Equal(
            [
                Guid.Parse("30000000-0000-0000-0000-000000000003"),
                Guid.Parse("40000000-0000-0000-0000-000000000004")
            ],
            remainingIds);
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
                processedAtUtc: new DateTimeOffset(2048, 5, 2, 1, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();
        var cleaner = new ProcessedIntegrationMessageCleaner(dbContext);

        int deletedCount = await cleaner.DeleteBatchAsync(
            processedBeforeUtc: new DateTimeOffset(2048, 5, 1, 23, 0, 0, TimeSpan.Zero),
            batchSize: 5,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, deletedCount);
        Assert.Equal(1, await dbContext.ProcessedIntegrationMessages.CountAsync());
    }
}
