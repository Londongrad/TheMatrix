using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Healthcare.Application.Care;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Healthcare.Infrastructure.Outbox;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Outbox;

public sealed class CareDeliveryActivityOutboxWriterTests
{
    [Fact]
    public async Task AddAsync_PersistsAggregatedCareDeliveryActivity()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var writer = new CareDeliveryActivityOutboxWriter(dbContext);
        DateTimeOffset occurredAtUtc =
            DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");
        var activity = new CareDeliveryActivitySnapshot(
            SimulationHostId: Guid.NewGuid(),
            SourceRevision: 17,
            CareDate: new DateOnly(2048, 5, 6),
            ProcessedPatientCount: 100,
            RoutineCareDeliveryCount: 4,
            UrgentCareDeliveryCount: 3,
            AcuteCareDeliveryCount: 2,
            EmergencyCareDeliveryCount: 1,
            OccurredAtUtc: occurredAtUtc,
            CorrelationId: "health-risk:17:care-delivery");

        await writer.AddAsync(activity);
        await dbContext.SaveChangesAsync();

        OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
        HealthcareCareDeliveryActivityV1? payload =
            JsonSerializer.Deserialize<HealthcareCareDeliveryActivityV1>(
                message.PayloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(HealthcareOutboxEventTypes.CareDeliveryActivityV1, message.Type);
        Assert.Equal(occurredAtUtc.UtcDateTime, message.OccurredOnUtc);
        Assert.NotNull(payload);
        Assert.Equal(100, payload.ProcessedPatientCount);
        Assert.Equal(4, payload.RoutineCareDeliveryCount);
        Assert.Equal(3, payload.UrgentCareDeliveryCount);
        Assert.Equal(2, payload.AcuteCareDeliveryCount);
        Assert.Equal(1, payload.EmergencyCareDeliveryCount);
    }
}
