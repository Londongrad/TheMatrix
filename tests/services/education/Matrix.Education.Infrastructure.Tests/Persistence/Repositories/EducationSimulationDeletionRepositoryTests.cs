using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Persistence.Models;
using Matrix.Education.Infrastructure.Persistence.Repositories;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class EducationSimulationDeletionRepositoryTests
    {
        private static readonly DateTimeOffset DeletedAtUtc =
            new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset UpdatedAtUtc = DeletedAtUtc.AddSeconds(1);

        [Fact]
        public async Task DeleteAndRecord_RemovesOnlyTargetHostDataAndKeepsTombstone()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var targetHostId = new SimulationHostId(Guid.NewGuid());
            var otherHostId = new SimulationHostId(Guid.NewGuid());
            AddHostData(dbContext, targetHostId);
            AddHostData(dbContext, otherHostId);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            var repository = new EducationSimulationDeletionRepository(dbContext);

            await repository.DeleteSimulationDataAsync(targetHostId);
            await repository.RecordAsync(
                simulationHostId: targetHostId,
                deletedAtUtc: DeletedAtUtc,
                updatedAtUtc: UpdatedAtUtc);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            Assert.DoesNotContain(
                dbContext.StudentProfiles,
                profile => profile.SimulationHostId == targetHostId);
            Assert.DoesNotContain(
                dbContext.Institutions,
                institution => institution.SimulationHostId == targetHostId);
            Assert.DoesNotContain(
                dbContext.Enrollments,
                enrollment => enrollment.SimulationHostId == targetHostId);
            Assert.DoesNotContain(
                dbContext.ProgressionCheckpoints,
                checkpoint => checkpoint.SimulationHostId == targetHostId);
            Assert.Contains(
                dbContext.StudentProfiles,
                profile => profile.SimulationHostId == otherHostId);
            Assert.Contains(
                dbContext.Institutions,
                institution => institution.SimulationHostId == otherHostId);
            Assert.Contains(
                dbContext.Enrollments,
                enrollment => enrollment.SimulationHostId == otherHostId);
            Assert.Contains(
                dbContext.ProgressionCheckpoints,
                checkpoint => checkpoint.SimulationHostId == otherHostId);

            EducationSimulationDeletionState tombstone = await dbContext.SimulationDeletionStates.SingleAsync();
            Assert.Equal(
                expected: targetHostId,
                actual: tombstone.SimulationHostId);
            Assert.Equal(
                expected: DeletedAtUtc,
                actual: tombstone.DeletedAtUtc);
            Assert.Equal(
                expected: UpdatedAtUtc,
                actual: tombstone.UpdatedAtUtc);
            Assert.Equal(
                expected: DeletedAtUtc,
                actual: await repository.GetDeletedAtUtcAsync(targetHostId));
        }

        private static void AddHostData(
            EducationDbContext dbContext,
            SimulationHostId simulationHostId)
        {
            var residentId = new ResidentId(Guid.NewGuid());
            EducationInstitution institution = EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: simulationHostId,
                name: "Education institution",
                kind: new EducationInstitutionKindKey("school"),
                capacity: 100);
            StudentProfile profile = StudentProfile.Register(
                residentId: residentId,
                simulationHostId: simulationHostId,
                birthDate: new DateOnly(2030, 5, 12),
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: DeletedAtUtc.AddYears(-1));
            StudentEnrollment enrollment = StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: simulationHostId,
                residentId: residentId,
                institutionId: institution.Id,
                stage: new EducationStageKey("secondary-school"),
                enrolledOn: new DateOnly(2047, 9, 1));
            EducationProgressionCheckpoint checkpoint = EducationProgressionCheckpoint.CreateCompleted(
                simulationHostId: simulationHostId,
                tickId: 10,
                completedAtUtc: DeletedAtUtc.AddDays(-1),
                updatedAtUtc: DeletedAtUtc.AddDays(-1));

            dbContext.AddRange(
                profile,
                institution,
                enrollment,
                checkpoint);
        }
    }
}
