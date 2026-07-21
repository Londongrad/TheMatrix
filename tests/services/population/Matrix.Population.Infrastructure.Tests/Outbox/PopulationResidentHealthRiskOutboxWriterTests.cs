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
    public sealed class PopulationResidentHealthRiskOutboxWriterTests
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        [Fact]
        public void PopulationContributor_ResolvesHealthRiskContract()
        {
            var registry = new OutboxEventTypeRegistry(
                [new PopulationOutboxEventTypeContributor()]);

            Type resolved = registry.Resolve(
                PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV1);

            Assert.Equal(typeof(PopulationResidentHealthRiskBatchV1), resolved);
        }

        [Fact]
        public async Task AddResidentHealthRiskBatchAsync_PersistsV2TypedPayload()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var writer = new PopulationResidentHealthRiskOutboxWriter(dbContext);
            DateTimeOffset observedAtUtc = new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);
            var batch = new PopulationResidentHealthRiskBatchV2(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 43,
                PreviousDate: new DateOnly(2048, 5, 5),
                CurrentDate: new DateOnly(2048, 5, 6),
                ObservedAtUtc: observedAtUtc,
                CorrelationId: "population:host:tick:43:health-risk",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents: []);

            await writer.AddResidentHealthRiskBatchAsync(batch);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            PopulationResidentHealthRiskBatchV2? payload =
                JsonSerializer.Deserialize<PopulationResidentHealthRiskBatchV2>(
                    message.PayloadJson,
                    JsonOptions);
            Assert.Equal(PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV2, message.Type);
            Assert.Equal(observedAtUtc.UtcDateTime, message.OccurredOnUtc);
            Assert.NotNull(payload);
            Assert.Equal(batch.SimulationHostId, payload.SimulationHostId);
            Assert.Equal(batch.SourceRevision, payload.SourceRevision);
            Assert.Empty(payload.Residents);
        }

        [Fact]
        public void PopulationContributor_ResolvesV2HealthRiskContract()
        {
            var registry = new OutboxEventTypeRegistry(
                [new PopulationOutboxEventTypeContributor()]);

            Type resolved = registry.Resolve(
                PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV2);

            Assert.Equal(typeof(PopulationResidentHealthRiskBatchV2), resolved);
        }
    }
}
