using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Healthcare.Infrastructure.Persistence;

namespace Matrix.Healthcare.Infrastructure.Outbox
{
    public sealed class PatientHealthOutcomeOutboxWriter(HealthcareDbContext dbContext)
        : IPatientHealthOutcomeOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public Task AddAsync(
            PatientHealthOutcomeBatch batch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(batch);

            var integrationEvent = new HealthcarePatientHealthOutcomeBatchV1(
                SimulationHostId: batch.SimulationHostId,
                SourceRevision: batch.SourceRevision,
                CurrentDate: batch.CurrentDate,
                OccurredAtUtc: batch.OccurredAtUtc,
                CorrelationId: batch.CorrelationId,
                BatchNumber: batch.BatchNumber,
                TotalBatches: batch.TotalBatches,
                Patients: batch.Patients
                   .Select(patient => new HealthcarePatientHealthOutcomeV1(
                        PatientId: patient.PatientId,
                        HealthScore: patient.HealthScore,
                        CurrentIllnessKind: patient.CurrentIllnessKind?.ToString(),
                        CurrentIllnessSeverity: patient.CurrentIllnessSeverity?.ToString(),
                        DiagnosedOn: patient.DiagnosedOn,
                        LastRecoveredOn: patient.LastRecoveredOn,
                        HealthDelta: patient.HealthDelta,
                        HappinessDelta: patient.HappinessDelta,
                        EnergyDelta: patient.EnergyDelta,
                        StressDelta: patient.StressDelta,
                        BecameCritical: patient.BecameCritical,
                        LifecycleRevision: patient.LifecycleRevision,
                        FunctionalCapacityScore: patient.FunctionalCapacityScore))
                   .ToArray());

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: HealthcareOutboxEventTypes.PatientHealthOutcomeBatchV1,
                    occurredOnUtc: batch.OccurredAtUtc.UtcDateTime,
                    payload: integrationEvent,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
