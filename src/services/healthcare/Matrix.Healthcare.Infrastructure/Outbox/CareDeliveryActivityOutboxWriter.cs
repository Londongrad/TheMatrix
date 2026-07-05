using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Care;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Healthcare.Infrastructure.Persistence;

namespace Matrix.Healthcare.Infrastructure.Outbox;

public sealed class CareDeliveryActivityOutboxWriter(HealthcareDbContext dbContext)
    : ICareDeliveryActivityOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task AddAsync(
        CareDeliveryActivitySnapshot activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var integrationEvent = new HealthcareCareDeliveryActivityV1(
            SimulationHostId: activity.SimulationHostId,
            SourceRevision: activity.SourceRevision,
            CareDate: activity.CareDate,
            ProcessedPatientCount: activity.ProcessedPatientCount,
            RoutineCareDeliveryCount: activity.RoutineCareDeliveryCount,
            UrgentCareDeliveryCount: activity.UrgentCareDeliveryCount,
            AcuteCareDeliveryCount: activity.AcuteCareDeliveryCount,
            EmergencyCareDeliveryCount: activity.EmergencyCareDeliveryCount,
            OccurredAtUtc: activity.OccurredAtUtc,
            CorrelationId: activity.CorrelationId);

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(
                type: HealthcareOutboxEventTypes.CareDeliveryActivityV1,
                occurredOnUtc: activity.OccurredAtUtc.UtcDateTime,
                payload: integrationEvent,
                jsonOptions: JsonOptions));

        return Task.CompletedTask;
    }
}
