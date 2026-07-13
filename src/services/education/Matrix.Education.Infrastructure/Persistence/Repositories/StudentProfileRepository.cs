using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Persistence.Repositories
{
    public sealed class StudentProfileRepository(EducationDbContext dbContext)
        : IStudentProfileRepository
    {
        public async Task<IReadOnlyList<StudentProfile>> ListBySimulationHostAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.StudentProfiles
               .Where(profile => profile.SimulationHostId == simulationHostId)
               .OrderBy(profile => profile.Id.Value)
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StudentProfile>> GetByIdsAsync(
            IReadOnlyCollection<ResidentId> residentIds,
            CancellationToken cancellationToken = default)
        {
            if (residentIds.Count == 0)
                return Array.Empty<StudentProfile>();

            return await dbContext.StudentProfiles
               .Where(profile => residentIds.Contains(profile.Id))
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<StudentProfile> profiles,
            CancellationToken cancellationToken = default)
        {
            dbContext.StudentProfiles.AddRange(profiles);
            return Task.CompletedTask;
        }
    }
}
