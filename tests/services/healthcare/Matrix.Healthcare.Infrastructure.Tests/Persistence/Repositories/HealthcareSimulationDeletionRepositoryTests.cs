using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class HealthcareSimulationDeletionRepositoryTests
    {
        [Fact]
        public async Task DeleteAndRecord_RemovesTargetPatientDataAndRetainsTombstone()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var deletedHostId = new SimulationHostId(Guid.NewGuid());
            var retainedHostId = new SimulationHostId(Guid.NewGuid());
            PatientProfile deletedProfile = CreateProfile(deletedHostId);
            PatientProfile retainedProfile = CreateProfile(retainedHostId);
            dbContext.PatientProfiles.AddRange(deletedProfile, retainedProfile);
            dbContext.PatientMedicalRecords.AddRange(
                CreateMedicalRecord(deletedProfile.PatientId, deletedHostId),
                CreateMedicalRecord(retainedProfile.PatientId, retainedHostId));
            dbContext.PatientCareNeeds.AddRange(
                CreateCareNeed(deletedProfile.PatientId, deletedHostId),
                CreateCareNeed(retainedProfile.PatientId, retainedHostId));
            dbContext.CareFacilities.AddRange(
                CreateFacility(deletedHostId),
                CreateFacility(retainedHostId));
            await dbContext.SaveChangesAsync();
            var repository = new HealthcareSimulationDeletionRepository(dbContext);
            DateTimeOffset deletedAtUtc = DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

            await repository.DeleteSimulationDataAsync(deletedHostId);
            await repository.RecordAsync(
                simulationHostId: deletedHostId,
                deletedAtUtc: deletedAtUtc,
                updatedAtUtc: deletedAtUtc.AddSeconds(1));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            Assert.False(await dbContext.PatientProfiles.AnyAsync(
                profile => profile.SimulationHostId == deletedHostId));
            Assert.True(await dbContext.PatientProfiles.AnyAsync(
                profile => profile.SimulationHostId == retainedHostId));
            Assert.False(await dbContext.PatientMedicalRecords.AnyAsync(
                record => record.SimulationHostId == deletedHostId));
            Assert.True(await dbContext.PatientMedicalRecords.AnyAsync(
                record => record.SimulationHostId == retainedHostId));
            Assert.False(await dbContext.PatientCareNeeds.AnyAsync(
                careNeed => careNeed.SimulationHostId == deletedHostId));
            Assert.True(await dbContext.PatientCareNeeds.AnyAsync(
                careNeed => careNeed.SimulationHostId == retainedHostId));
            Assert.False(await dbContext.CareFacilities.AnyAsync(
                facility => facility.SimulationHostId == deletedHostId));
            Assert.True(await dbContext.CareFacilities.AnyAsync(
                facility => facility.SimulationHostId == retainedHostId));
            Assert.Equal(
                deletedAtUtc,
                await repository.GetDeletedAtUtcAsync(deletedHostId));
        }

        private static PatientProfile CreateProfile(SimulationHostId simulationHostId)
        {
            return PatientProfile.Register(
                patientId: new PatientId(Guid.NewGuid()),
                simulationHostId: simulationHostId,
                birthDate: new DateOnly(2030, 5, 12),
                sex: PatientSex.Female,
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T09:00:00+00:00"));
        }

        private static PatientMedicalRecord CreateMedicalRecord(
            PatientId patientId,
            SimulationHostId simulationHostId)
        {
            return PatientMedicalRecord.Register(
                patientId,
                simulationHostId,
                HealthScore.Full,
                PatientIllnessState.Healthy());
        }

        private static CareFacility CreateFacility(SimulationHostId simulationHostId)
        {
            return CareFacility.Register(
                id: CareFacilityId.New(),
                simulationHostId: simulationHostId,
                name: "Central Hospital",
                kind: new CareFacilityKindKey("Hospital"),
                locationAnchorId: null,
                dailyPatientCapacity: 240,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T09:00:00+00:00"));
        }

        private static PatientCareNeed CreateCareNeed(
            PatientId patientId,
            SimulationHostId simulationHostId)
        {
            return PatientCareNeed.Register(
                patientId: patientId,
                simulationHostId: simulationHostId,
                urgency: CareNeedUrgency.Urgent,
                requestedOn: new DateOnly(2048, 5, 6),
                assessmentRevision: 1,
                lifecycleRevision: 0,
                assessedAtUtc: DateTimeOffset.Parse("2048-05-06T09:00:00+00:00"));
        }
    }
}
