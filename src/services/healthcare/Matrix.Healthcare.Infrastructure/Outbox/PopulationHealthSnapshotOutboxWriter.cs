using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Operations;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Healthcare.Infrastructure.Persistence;

namespace Matrix.Healthcare.Infrastructure.Outbox;

public sealed class PopulationHealthSnapshotOutboxWriter(HealthcareDbContext dbContext)
    : IPopulationHealthSnapshotOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task AddAsync(
        PopulationHealthSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Pressure);

        var integrationEvent = new HealthcarePopulationHealthSnapshotV1(
            SimulationHostId: snapshot.SimulationHostId,
            SourceRevision: snapshot.SourceRevision,
            CurrentDate: snapshot.CurrentDate,
            PatientCount: snapshot.Pressure.PatientCount,
            ActiveIllnessCount: snapshot.Pressure.ActiveIllnessCount,
            SevereIllnessCount: snapshot.Pressure.SevereIllnessCount,
            MedicalLoadIndex: snapshot.Pressure.MedicalLoadIndex,
            TriagePressureIndex: snapshot.Pressure.TriagePressureIndex,
            RecoverySupportIndex: snapshot.Pressure.RecoverySupportIndex,
            OccurredAtUtc: snapshot.OccurredAtUtc,
            CorrelationId: snapshot.CorrelationId);

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(
                type: HealthcareOutboxEventTypes.PopulationHealthSnapshotV1,
                occurredOnUtc: snapshot.OccurredAtUtc.UtcDateTime,
                payload: integrationEvent,
                jsonOptions: JsonOptions));

        return Task.CompletedTask;
    }
}
