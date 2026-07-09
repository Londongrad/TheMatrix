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
        public async Task AddResidentHealthRiskBatchAsync_PersistsTypedPayload()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var writer = new PopulationResidentHealthRiskOutboxWriter(dbContext);
            DateTimeOffset observedAtUtc = new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);
            var batch = new PopulationResidentHealthRiskBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 42,
                PreviousDate: new DateOnly(2048, 5, 5),
                CurrentDate: new DateOnly(2048, 5, 6),
                ObservedAtUtc: observedAtUtc,
                CorrelationId: "population:host:tick:42:health-risk",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentHealthRiskV1(
                        ResidentId: Guid.NewGuid(),
                        EnergyScore: 61,
                        HappinessScore: 62,
                        StressScore: 38,
                        SocialNeedScore: 23,
                        IsVulnerable: false,
                        HousingStability: "Housed",
                        HasStructuredDailyActivity: true,
                        HouseholdSize: 3,
                        CaregiverSupportStrength: 0.12d,
                        HadAdverseWeatherExposure: false,
                        HealthcareSupportStrength: 0.51d,
                        PublicHealthRiskStrength: 0.17d,
                        CommunityId: Guid.NewGuid())
                ]);

            await writer.AddResidentHealthRiskBatchAsync(batch);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            PopulationResidentHealthRiskBatchV1? payload =
                JsonSerializer.Deserialize<PopulationResidentHealthRiskBatchV1>(
                    message.PayloadJson,
                    JsonOptions);
            Assert.Equal(PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV1, message.Type);
            Assert.Equal(observedAtUtc.UtcDateTime, message.OccurredOnUtc);
            Assert.NotNull(payload);
            Assert.Equal(batch.Residents, payload.Residents);
        }

        [Fact]
        public void PopulationContributor_ResolvesHealthRiskContract()
        {
            var registry = new OutboxEventTypeRegistry(
                [new PopulationOutboxEventTypeContributor()]);

            Type resolved = registry.Resolve(
                PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV1);

            Assert.Equal(typeof(PopulationResidentHealthRiskBatchV1), resolved);
        }
    }
}
