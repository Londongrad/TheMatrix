using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Persistence.Repositories
{
    public sealed class EducationInstitutionRepository(EducationDbContext dbContext)
        : IEducationInstitutionRepository
    {
        public Task<EducationInstitution?> GetAsync(
            SimulationHostId simulationHostId,
            EducationInstitutionId institutionId,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Institutions.SingleOrDefaultAsync(
                institution => institution.SimulationHostId == simulationHostId
                               && institution.Id == institutionId,
                cancellationToken);
        }

        public async Task<IReadOnlyList<EducationInstitution>> ListAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.Institutions
               .Where(institution => institution.SimulationHostId == simulationHostId)
               .OrderBy(institution => institution.Name)
               .ThenBy(institution => institution.Id)
               .ToListAsync(cancellationToken);
        }

        public Task AddAsync(
            EducationInstitution institution,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(institution);
            dbContext.Institutions.Add(institution);
            return Task.CompletedTask;
        }
    }
}
