using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class ProcessedIntegrationMessageRepositoryTests
    {
        [Fact]
        public async Task TryMarkProcessedAsync_WhenMarkerIsNew_ReturnsTrueAndPersists()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var repository = new ProcessedIntegrationMessageRepository(dbContext);
            var messageId = Guid.Parse("1105fd74-7056-412b-b35b-0a1df70ef33e");
            DateTimeOffset processedAtUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 10,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            bool inserted = await repository.TryMarkProcessedAsync(
                consumer: "CityTimeAdvancedConsumer",
                messageId: messageId,
                processedAtUtc: processedAtUtc);

            Assert.True(inserted);

            ProcessedIntegrationMessage marker = await dbContext.ProcessedIntegrationMessages.SingleAsync();
            Assert.Equal(
                expected: "CityTimeAdvancedConsumer",
                actual: marker.Consumer);
            Assert.Equal(
                expected: messageId,
                actual: marker.MessageId);
            Assert.Equal(
                expected: processedAtUtc,
                actual: marker.ProcessedAtUtc);
        }

        [Fact]
        public async Task TryMarkProcessedAsync_WhenMarkerAlreadyExists_ReturnsFalse()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var messageId = Guid.Parse("7a76c6dd-4b2d-4eff-8b63-ac8564cb9615");
            dbContext.ProcessedIntegrationMessages.Add(
                new ProcessedIntegrationMessage(
                    consumer: "CityCreatedConsumer",
                    messageId: messageId,
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            await dbContext.SaveChangesAsync();
            var repository = new ProcessedIntegrationMessageRepository(dbContext);

            bool inserted = await repository.TryMarkProcessedAsync(
                consumer: "CityCreatedConsumer",
                messageId: messageId,
                processedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            Assert.False(inserted);
            Assert.Equal(
                expected: 1,
                actual: await dbContext.ProcessedIntegrationMessages.CountAsync());
        }

        [Fact]
        public async Task TryMarkProcessedAsync_WhenConsumerIsBlank_ThrowsArgumentException()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            var repository = new ProcessedIntegrationMessageRepository(database.DbContext);

            await Assert.ThrowsAsync<ArgumentException>(() => repository.TryMarkProcessedAsync(
                consumer: " ",
                messageId: Guid.Parse("69eb3a0d-a315-42fc-b664-cc2199a2afe1"),
                processedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)));
        }

        [Fact]
        public async Task TryMarkProcessedAsync_WhenMessageIdIsEmpty_ThrowsArgumentException()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            var repository = new ProcessedIntegrationMessageRepository(database.DbContext);

            await Assert.ThrowsAsync<ArgumentException>(() => repository.TryMarkProcessedAsync(
                consumer: "CityCreatedConsumer",
                messageId: Guid.Empty,
                processedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)));
        }

        [Fact]
        public async Task TryMarkProcessedAsync_WhenProcessedAtUtcIsNotUtc_ThrowsArgumentException()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            var repository = new ProcessedIntegrationMessageRepository(database.DbContext);

            await Assert.ThrowsAsync<ArgumentException>(() => repository.TryMarkProcessedAsync(
                consumer: "CityCreatedConsumer",
                messageId: Guid.Parse("5f9c349d-a682-4bd4-a6d7-b266dd7e08fe"),
                processedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3))));
        }
    }
}
