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
        public async Task DeleteAndRecord_RemovesOnlyTargetProfilesAndRetainsTombstone()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            var deletedHostId = new SimulationHostId(Guid.NewGuid());
            var retainedHostId = new SimulationHostId(Guid.NewGuid());
            PatientProfile deletedProfile = CreateProfile(deletedHostId);
            PatientProfile retainedProfile = CreateProfile(retainedHostId);
            dbContext.PatientProfiles.AddRange(deletedProfile, retainedProfile);
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
    }
}
