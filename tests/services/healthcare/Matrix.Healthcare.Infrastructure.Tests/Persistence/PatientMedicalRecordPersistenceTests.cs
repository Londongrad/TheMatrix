using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence
{
    public sealed class PatientMedicalRecordPersistenceTests
    {
        [Fact]
        public async Task SaveAndReload_PreservesMedicalState()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var patientId = new PatientId(Guid.NewGuid());
            PatientMedicalRecord record = PatientMedicalRecord.Register(
                patientId,
                new SimulationHostId(Guid.NewGuid()),
                new HealthScore(64),
                PatientIllnessState.Active(
                    IllnessKind.Infection,
                    IllnessSeverity.Moderate,
                    diagnosedOn: new DateOnly(2048, 5, 6),
                    lastRecoveredOn: new DateOnly(2048, 4, 28)));
            record.TryAcceptProgressionRevision(17);

            dbContext.PatientMedicalRecords.Add(record);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            PatientMedicalRecord? loaded = await dbContext.PatientMedicalRecords.FindAsync(patientId);

            Assert.NotNull(loaded);
            Assert.Equal(64, loaded.Health.Value);
            Assert.Equal(IllnessKind.Infection, loaded.Illness.CurrentKind);
            Assert.Equal(IllnessSeverity.Moderate, loaded.Illness.CurrentSeverity);
            Assert.Equal(new DateOnly(2048, 5, 6), loaded.Illness.DiagnosedOn);
            Assert.Equal(new DateOnly(2048, 4, 28), loaded.Illness.LastRecoveredOn);
            Assert.Equal(17, loaded.LastProgressionRevision);
        }
    }
}
