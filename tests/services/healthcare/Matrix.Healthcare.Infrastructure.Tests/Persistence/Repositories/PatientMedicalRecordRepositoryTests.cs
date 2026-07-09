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

        [Fact]
        public async Task GetCommunityHealthBurdens_AggregatesAssignedPatientsByCommunity()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new PatientMedicalRecordRepository(dbContext);
            var simulationHostId = new SimulationHostId(Guid.NewGuid());
            var firstCommunityId = new PatientCommunityId(Guid.NewGuid());
            var secondCommunityId = new PatientCommunityId(Guid.NewGuid());
            PatientMedicalRecord firstMild = CreateRecord(Guid.NewGuid(), simulationHostId);
            firstMild.TryAcceptProgressionRevision(1, firstCommunityId);
            firstMild.DiagnoseIllness(
                IllnessKind.Infection,
                IllnessSeverity.Mild,
                new DateOnly(2048, 5, 6));
            PatientMedicalRecord firstSevere = CreateRecord(Guid.NewGuid(), simulationHostId);
            firstSevere.TryAcceptProgressionRevision(1, firstCommunityId);
            firstSevere.DiagnoseIllness(
                IllnessKind.Exposure,
                IllnessSeverity.Severe,
                new DateOnly(2048, 5, 6));
            PatientMedicalRecord secondHealthy = CreateRecord(Guid.NewGuid(), simulationHostId);
            secondHealthy.TryAcceptProgressionRevision(1, secondCommunityId);

            dbContext.PatientMedicalRecords.AddRange(
                firstMild,
                firstSevere,
                secondHealthy,
                CreateRecord(Guid.NewGuid(), simulationHostId),
                CreateRecord(Guid.NewGuid(), new SimulationHostId(Guid.NewGuid())));
            await dbContext.SaveChangesAsync();

            IReadOnlyList<PatientCommunityHealthBurden> burdens =
                await repository.GetCommunityHealthBurdensAsync(simulationHostId);

            Assert.Equal(2, burdens.Count);
            PatientCommunityHealthBurden first = Assert.Single(
                burdens,
                burden => burden.CommunityId == firstCommunityId);
            Assert.Equal(2, first.Burden.PatientCount);
            Assert.Equal(2, first.Burden.ActiveIllnessCount);
            Assert.Equal(1, first.Burden.SevereIllnessCount);
            PatientCommunityHealthBurden second = Assert.Single(
                burdens,
                burden => burden.CommunityId == secondCommunityId);
            Assert.Equal(1, second.Burden.PatientCount);
            Assert.Equal(0, second.Burden.ActiveIllnessCount);
        }

        [Fact]
        public async Task GetInfectiousPatientCountsByHousehold_AggregatesEligiblePatientRecords()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var repository = new PatientMedicalRecordRepository(dbContext);
            var simulationHostId = new SimulationHostId(Guid.NewGuid());
            var firstHouseholdId = new PatientHouseholdId(Guid.NewGuid());
            var secondHouseholdId = new PatientHouseholdId(Guid.NewGuid());
            PatientMedicalRecord firstInfection = CreateRecord(Guid.NewGuid(), simulationHostId);
            firstInfection.DiagnoseIllness(
                IllnessKind.Infection,
                IllnessSeverity.Mild,
                new DateOnly(2048, 5, 6));
            PatientMedicalRecord secondInfection = CreateRecord(Guid.NewGuid(), simulationHostId);
            secondInfection.DiagnoseIllness(
                IllnessKind.Infection,
                IllnessSeverity.Severe,
                new DateOnly(2048, 5, 6));
            PatientMedicalRecord exposure = CreateRecord(Guid.NewGuid(), simulationHostId);
            exposure.DiagnoseIllness(
                IllnessKind.Exposure,
                IllnessSeverity.Moderate,
                new DateOnly(2048, 5, 6));

            dbContext.PatientProfiles.AddRange(
                CreateProfile(firstInfection.PatientId, simulationHostId, firstHouseholdId),
                CreateProfile(secondInfection.PatientId, simulationHostId, firstHouseholdId),
                CreateProfile(exposure.PatientId, simulationHostId, secondHouseholdId));
            dbContext.PatientMedicalRecords.AddRange(firstInfection, secondInfection, exposure);
            await dbContext.SaveChangesAsync();

            IReadOnlyDictionary<PatientHouseholdId, int> counts =
                await repository.GetInfectiousPatientCountsByHouseholdAsync(
                    simulationHostId,
                    [firstHouseholdId, secondHouseholdId]);

            Assert.Equal(2, Assert.Single(counts).Value);
            Assert.Equal(2, counts[firstHouseholdId]);
            Assert.False(counts.ContainsKey(secondHouseholdId));
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

        private static PatientProfile CreateProfile(
            PatientId patientId,
            SimulationHostId simulationHostId,
            PatientHouseholdId householdId)
        {
            return PatientProfile.Register(
                patientId,
                simulationHostId,
                birthDate: new DateOnly(2030, 5, 6),
                sex: PatientSex.Female,
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                householdId: householdId);
        }
    }
}
