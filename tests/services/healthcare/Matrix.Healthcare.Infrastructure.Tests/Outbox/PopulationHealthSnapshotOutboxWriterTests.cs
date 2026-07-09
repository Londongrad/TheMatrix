using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Healthcare.Application.Operations;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Infrastructure.Outbox;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Outbox;

public sealed class PopulationHealthSnapshotOutboxWriterTests
{
    [Fact]
    public async Task AddAsync_PersistsHealthcareOwnedPopulationSnapshot()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var writer = new PopulationHealthSnapshotOutboxWriter(dbContext);
        DateTimeOffset occurredAtUtc =
            DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");
        Guid communityId = Guid.NewGuid();
        var snapshot = new PopulationHealthSnapshot(
            SimulationHostId: Guid.NewGuid(),
            SourceRevision: 17,
            CurrentDate: new DateOnly(2048, 5, 6),
            Pressure: new CareSystemPressureProfile(
                PatientCount: 100,
                ActiveIllnessCount: 8,
                SevereIllnessCount: 2,
                MedicalLoadIndex: 0.82m,
                TriagePressureIndex: 0.34m,
                RecoverySupportIndex: 1.12m),
            Communities:
            [
                new CommunityHealthSnapshot(
                    CommunityId: communityId,
                    PatientCount: 40,
                    ActiveIllnessCount: 5,
                    SevereIllnessCount: 1)
            ],
            OccurredAtUtc: occurredAtUtc,
            CorrelationId: "health-risk:17:population-health");

        await writer.AddAsync(snapshot);
        await dbContext.SaveChangesAsync();

        OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
        HealthcarePopulationHealthSnapshotV1? payload =
            JsonSerializer.Deserialize<HealthcarePopulationHealthSnapshotV1>(
                message.PayloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(HealthcareOutboxEventTypes.PopulationHealthSnapshotV1, message.Type);
        Assert.Equal(occurredAtUtc.UtcDateTime, message.OccurredOnUtc);
        Assert.NotNull(payload);
        Assert.Equal(100, payload.PatientCount);
        Assert.Equal(8, payload.ActiveIllnessCount);
        Assert.Equal(2, payload.SevereIllnessCount);
        Assert.Equal(0.82m, payload.MedicalLoadIndex);
        HealthcareCommunityHealthSnapshotV1 community = Assert.Single(payload.Communities!);
        Assert.Equal(communityId, community.CommunityId);
        Assert.Equal(40, community.PatientCount);
        Assert.Equal(5, community.ActiveIllnessCount);
        Assert.Equal(1, community.SevereIllnessCount);
    }
}
