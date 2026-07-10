using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Persistence.Repositories;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class StudentEnrollmentRepositoryTests
    {
        [Fact]
        public async Task ActiveQueries_ExcludeClosedAndForeignEnrollments()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var repository = new StudentEnrollmentRepository(dbContext);
            var simulationHostId = new SimulationHostId(Guid.NewGuid());
            var residentId = new ResidentId(Guid.NewGuid());
            StudentEnrollment active = CreateEnrollment(simulationHostId, residentId);
            StudentEnrollment completed = CreateEnrollment(
                simulationHostId,
                new ResidentId(Guid.NewGuid()));
            completed.Complete(new DateOnly(2048, 6, 1));
            StudentEnrollment foreign = CreateEnrollment(
                new SimulationHostId(Guid.NewGuid()),
                new ResidentId(Guid.NewGuid()));

            await repository.AddAsync(active);
            await repository.AddAsync(completed);
            await repository.AddAsync(foreign);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            StudentEnrollment? loaded = await repository.GetActiveByResidentAsync(
                simulationHostId,
                residentId);
            IReadOnlyList<StudentEnrollment> listed =
                await repository.ListActiveAsync(simulationHostId);

            Assert.NotNull(loaded);
            Assert.Equal(active.EnrollmentId, loaded.EnrollmentId);
            Assert.Equal(active.EnrollmentId, Assert.Single(listed).EnrollmentId);
        }

        private static StudentEnrollment CreateEnrollment(
            SimulationHostId simulationHostId,
            ResidentId residentId)
        {
            return StudentEnrollment.Enroll(
                id: new EnrollmentId(Guid.NewGuid()),
                simulationHostId: simulationHostId,
                residentId: residentId,
                institutionId: new EducationInstitutionId(Guid.NewGuid()),
                stage: new EducationStageKey("secondary"),
                enrolledOn: new DateOnly(2048, 5, 1));
        }
    }
}
