using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Infrastructure.Outbox;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Outbox
{
    public sealed class PatientHealthOutcomeOutboxWriterTests
    {
        [Fact]
        public async Task AddAsync_PersistsTypedHealthOutcomeBatch()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var writer = new PatientHealthOutcomeOutboxWriter(dbContext);
            DateTimeOffset occurredAtUtc = DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");
            var patientId = Guid.NewGuid();
            var batch = new PatientHealthOutcomeBatch(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 17,
                OccurredAtUtc: occurredAtUtc,
                CorrelationId: "health-risk:17:outcome",
                BatchNumber: 1,
                TotalBatches: 1,
                Patients:
                [
                    new PatientHealthProgressionResultItem(
                        PatientId: patientId,
                        HealthScore: 64,
                        CurrentIllnessKind: IllnessKind.Infection,
                        CurrentIllnessSeverity: IllnessSeverity.Moderate,
                        DiagnosedOn: new DateOnly(2048, 5, 4),
                        LastRecoveredOn: null,
                        HealthDelta: -2,
                        HappinessDelta: -2,
                        EnergyDelta: -2,
                        StressDelta: 2,
                        BecameCritical: false)
                ]);

            await writer.AddAsync(batch);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            HealthcarePatientHealthOutcomeBatchV1? payload =
                JsonSerializer.Deserialize<HealthcarePatientHealthOutcomeBatchV1>(
                    message.PayloadJson,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.Equal(HealthcareOutboxEventTypes.PatientHealthOutcomeBatchV1, message.Type);
            Assert.Equal(occurredAtUtc.UtcDateTime, message.OccurredOnUtc);
            Assert.NotNull(payload);
            HealthcarePatientHealthOutcomeV1 patient = Assert.Single(payload.Patients);
            Assert.Equal(patientId, patient.PatientId);
            Assert.Equal("Infection", patient.CurrentIllnessKind);
            Assert.Equal(-2, patient.HealthDelta);
        }
    }
}
