using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Persistence.Repositories
{
    public sealed class StudentEnrollmentRepository(EducationDbContext dbContext)
        : IStudentEnrollmentRepository
    {
        public Task<StudentEnrollment?> GetActiveByResidentAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Enrollments.SingleOrDefaultAsync(
                enrollment => enrollment.SimulationHostId == simulationHostId
                              && enrollment.ResidentId == residentId
                              && enrollment.Status == EnrollmentStatus.Active,
                cancellationToken);
        }

        public async Task<IReadOnlyList<StudentEnrollment>> ListActiveAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.Enrollments
               .Where(enrollment => enrollment.SimulationHostId == simulationHostId
                                    && enrollment.Status == EnrollmentStatus.Active)
               .OrderBy(enrollment => enrollment.Id)
               .ToListAsync(cancellationToken);
        }

        public Task AddAsync(
            StudentEnrollment enrollment,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(enrollment);
            dbContext.Enrollments.Add(enrollment);
            return Task.CompletedTask;
        }
    }
}
