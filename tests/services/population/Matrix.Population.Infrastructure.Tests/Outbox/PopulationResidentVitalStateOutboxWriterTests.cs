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
    public sealed class PopulationResidentVitalStateOutboxWriterTests
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task AddResidentVitalStateBatchAsync_PersistsTypedPayload()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var writer = new PopulationResidentVitalStateOutboxWriter(dbContext);
            DateTimeOffset observedAtUtc = new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);
            var batch = new PopulationResidentVitalStateBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 42,
                ObservedAtUtc: observedAtUtc,
                CorrelationId: "population:host:tick:42:vital-state",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentVitalStateV1(
                        ResidentId: Guid.NewGuid(),
                        HealthScore: 62,
                        LifecycleRevision: 3)
                ]);

            await writer.AddResidentVitalStateBatchAsync(batch);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            PopulationResidentVitalStateBatchV1? payload =
                JsonSerializer.Deserialize<PopulationResidentVitalStateBatchV1>(
                    message.PayloadJson,
                    JsonOptions);
            Assert.Equal(PopulationOutboxEventTypes.PopulationResidentVitalStateBatchV1, message.Type);
            Assert.Equal(observedAtUtc.UtcDateTime, message.OccurredOnUtc);
            Assert.NotNull(payload);
            Assert.Equal(batch.Residents, payload.Residents);
        }

        [Fact]
        public void PopulationContributor_ResolvesVitalStateContract()
        {
            var registry = new OutboxEventTypeRegistry(
                [new PopulationOutboxEventTypeContributor()]);

            Type resolved = registry.Resolve(
                PopulationOutboxEventTypes.PopulationResidentVitalStateBatchV1);

            Assert.Equal(typeof(PopulationResidentVitalStateBatchV1), resolved);
        }
    }
}
