using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Infrastructure.Outbox;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Outbox
{
    public sealed class PopulationResidentFactsOutboxWriterTests
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task AddResidentFactsBatchAsync_PersistsTypedPayload()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var writer = new PopulationResidentFactsOutboxWriter(dbContext);
            DateTimeOffset synchronizedAtUtc = new(
                2048, 5, 6, 10, 0, 0, TimeSpan.Zero);
            var batch = new PopulationResidentFactsBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 42,
                SynchronizedAtUtc: synchronizedAtUtc,
                CorrelationId: "population:host:tick:42:resident-facts",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentFactsV1(
                        ResidentId: Guid.NewGuid(),
                        BirthDate: new DateOnly(2030, 4, 2),
                        Sex: "Female",
                        IsAlive: true,
                        IsActive: true,
                        HouseholdId: Guid.NewGuid())
                ]);

            await writer.AddResidentFactsBatchAsync(batch);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            PopulationResidentFactsBatchV1? payload =
                JsonSerializer.Deserialize<PopulationResidentFactsBatchV1>(
                    json: message.PayloadJson,
                    options: JsonOptions);
            Assert.Equal(PopulationOutboxEventTypes.PopulationResidentFactsBatchV1, message.Type);
            Assert.Equal(synchronizedAtUtc.UtcDateTime, message.OccurredOnUtc);
            Assert.NotNull(payload);
            Assert.Equal(batch.SimulationHostId, payload.SimulationHostId);
            Assert.Equal(42, payload.SourceRevision);
            Assert.Equal(batch.Residents, payload.Residents);
        }

        [Fact]
        public void PopulationContributor_ResolvesResidentFactContract()
        {
            var registry = new OutboxEventTypeRegistry(
                [new PopulationOutboxEventTypeContributor()]);

            Type resolved = registry.Resolve(
                PopulationOutboxEventTypes.PopulationResidentFactsBatchV1);

            Assert.Equal(typeof(PopulationResidentFactsBatchV1), resolved);
        }
    }
}
