using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class PatientMedicalRecordRepositoryTests
    {
        [Fact]
        public async Task AddRangeAndGetByIds_LoadsOnlyRequestedRecords()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new PatientMedicalRecordRepository(dbContext);
            PatientMedicalRecord requested = CreateRecord(Guid.NewGuid());
            PatientMedicalRecord unrequested = CreateRecord(Guid.NewGuid());

            await repository.AddRangeAsync(new[] { requested, unrequested });
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<PatientMedicalRecord> loaded = await repository.GetByIdsAsync(
                new[] { requested.PatientId });

            PatientMedicalRecord record = Assert.Single(loaded);
            Assert.Equal(requested.PatientId, record.PatientId);
        }

        [Fact]
        public async Task GetByIds_WhenIdsAreEmpty_DoesNotTrackRecords()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new PatientMedicalRecordRepository(dbContext);
            dbContext.PatientMedicalRecords.Add(CreateRecord(Guid.NewGuid()));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<PatientMedicalRecord> loaded = await repository.GetByIdsAsync(
                Array.Empty<PatientId>());

            Assert.Empty(loaded);
            Assert.Empty(dbContext.ChangeTracker.Entries<PatientMedicalRecord>());
        }

        [Fact]
        public async Task GetPopulationHealthBurden_AggregatesOnlyRequestedSimulation()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new PatientMedicalRecordRepository(dbContext);
            var simulationHostId = new SimulationHostId(Guid.NewGuid());
            PatientMedicalRecord mild = CreateRecord(Guid.NewGuid(), simulationHostId);
            mild.DiagnoseIllness(IllnessKind.Infection, IllnessSeverity.Mild, new DateOnly(2048, 5, 6));
            PatientMedicalRecord severe = CreateRecord(Guid.NewGuid(), simulationHostId);
            severe.DiagnoseIllness(IllnessKind.Exposure, IllnessSeverity.Severe, new DateOnly(2048, 5, 6));

            dbContext.PatientMedicalRecords.AddRange(
                mild,
                severe,
                CreateRecord(Guid.NewGuid(), simulationHostId),
                CreateRecord(Guid.NewGuid(), new SimulationHostId(Guid.NewGuid())));
            await dbContext.SaveChangesAsync();

            PatientPopulationHealthBurden burden =
                await repository.GetPopulationHealthBurdenAsync(simulationHostId);

            Assert.Equal(3, burden.PatientCount);
            Assert.Equal(2, burden.ActiveIllnessCount);
            Assert.Equal(1, burden.MildIllnessCount);
            Assert.Equal(1, burden.SevereIllnessCount);
        }

        private static PatientMedicalRecord CreateRecord(
            Guid patientId,
            SimulationHostId? simulationHostId = null)
        {
            return PatientMedicalRecord.Register(
                new PatientId(patientId),
                simulationHostId ?? new SimulationHostId(Guid.NewGuid()),
                HealthScore.Full,
                PatientIllnessState.Healthy());
        }
    }
}
