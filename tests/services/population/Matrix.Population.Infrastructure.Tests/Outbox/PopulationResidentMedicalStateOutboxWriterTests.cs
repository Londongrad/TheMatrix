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
    public sealed class PopulationResidentMedicalStateOutboxWriterTests
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task AddResidentMedicalStateBatchAsync_PersistsTypedPayload()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var writer = new PopulationResidentMedicalStateOutboxWriter(dbContext);
            DateTimeOffset observedAtUtc = new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);
            var batch = new PopulationResidentMedicalStateBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 42,
                ObservedAtUtc: observedAtUtc,
                CorrelationId: "population:host:tick:42:medical-state",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentMedicalStateV1(
                        ResidentId: Guid.NewGuid(),
                        HealthScore: 62,
                        CurrentIllnessKind: "Infection",
                        CurrentIllnessSeverity: "Moderate",
                        DiagnosedOn: new DateOnly(2048, 5, 5),
                        LastRecoveredOn: null)
                ]);

            await writer.AddResidentMedicalStateBatchAsync(batch);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            PopulationResidentMedicalStateBatchV1? payload =
                JsonSerializer.Deserialize<PopulationResidentMedicalStateBatchV1>(
                    message.PayloadJson,
                    JsonOptions);
            Assert.Equal(PopulationOutboxEventTypes.PopulationResidentMedicalStateBatchV1, message.Type);
            Assert.Equal(observedAtUtc.UtcDateTime, message.OccurredOnUtc);
            Assert.NotNull(payload);
            Assert.Equal(batch.Residents, payload.Residents);
        }

        [Fact]
        public void PopulationContributor_ResolvesMedicalStateContract()
        {
            var registry = new OutboxEventTypeRegistry(
                [new PopulationOutboxEventTypeContributor()]);

            Type resolved = registry.Resolve(
                PopulationOutboxEventTypes.PopulationResidentMedicalStateBatchV1);

            Assert.Equal(typeof(PopulationResidentMedicalStateBatchV1), resolved);
        }
    }
}
