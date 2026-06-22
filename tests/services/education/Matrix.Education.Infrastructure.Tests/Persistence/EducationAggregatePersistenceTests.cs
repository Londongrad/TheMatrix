using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence
{
    public sealed class EducationAggregatePersistenceTests
    {
        [Fact]
        public async Task SaveAndReload_PreservesEducationOwnedAggregates()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var hostId = new SimulationHostId(Guid.NewGuid());
            var residentId = new ResidentId(Guid.NewGuid());
            EducationInstitution institution = EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: hostId,
                name: "Central University",
                kind: new EducationInstitutionKindKey("university"),
                capacity: 1000,
                locationAnchorId: new LocationAnchorId(Guid.NewGuid()));
            StudentProfile profile = StudentProfile.Register(
                residentId: residentId,
                simulationHostId: hostId,
                birthDate: new DateOnly(2028, 5, 12),
                isAlive: true,
                isActive: true,
                sourceRevision: 4,
                synchronizedAtUtc: new DateTimeOffset(2048, 1, 1, 0, 0, 0, TimeSpan.Zero));
            StudentEnrollment enrollment = StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: hostId,
                residentId: residentId,
                institutionId: institution.Id,
                stage: new EducationStageKey("higher-education"),
                enrolledOn: new DateOnly(2047, 9, 1));
            institution.TryReserveSeats(1);

            dbContext.AddRange(profile, institution, enrollment);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            StudentProfile loadedProfile = await dbContext.StudentProfiles.SingleAsync();
            EducationInstitution loadedInstitution = await dbContext.Institutions.SingleAsync();
            StudentEnrollment loadedEnrollment = await dbContext.Enrollments.SingleAsync();

            Assert.Equal(residentId, loadedProfile.ResidentId);
            Assert.Equal(hostId, loadedProfile.SimulationHostId);
            Assert.Equal(4, loadedProfile.LastSourceRevision);
            Assert.Equal(1, loadedInstitution.CurrentEnrollmentCount);
            Assert.Equal("university", loadedInstitution.Kind.Value);
            Assert.Equal(residentId, loadedEnrollment.ResidentId);
            Assert.Equal(institution.Id, loadedEnrollment.InstitutionId);
            Assert.Equal("higher-education", loadedEnrollment.Stage.Value);
            Assert.Equal(EnrollmentStatus.Active, loadedEnrollment.Status);
        }

        [Fact]
        public async Task SaveAndReload_PreservesTerminalEnrollmentState()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var hostId = new SimulationHostId(Guid.NewGuid());
            var residentId = new ResidentId(Guid.NewGuid());
            EducationInstitution institution = EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: hostId,
                name: "North School",
                kind: new EducationInstitutionKindKey("school"),
                capacity: 500);
            StudentProfile profile = StudentProfile.Register(
                residentId: residentId,
                simulationHostId: hostId,
                birthDate: new DateOnly(2038, 2, 3),
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: new DateTimeOffset(2048, 1, 1, 0, 0, 0, TimeSpan.Zero));
            StudentEnrollment enrollment = StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: hostId,
                residentId: residentId,
                institutionId: institution.Id,
                stage: new EducationStageKey("secondary-school"),
                enrolledOn: new DateOnly(2047, 9, 1));
            enrollment.Complete(new DateOnly(2048, 6, 1));

            dbContext.AddRange(profile, institution, enrollment);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            StudentEnrollment loaded = await dbContext.Enrollments.SingleAsync();

            Assert.Equal(EnrollmentStatus.Completed, loaded.Status);
            Assert.Equal(new DateOnly(2048, 6, 1), loaded.ClosedOn);
            Assert.False(loaded.IsActive);
        }
    }
}
