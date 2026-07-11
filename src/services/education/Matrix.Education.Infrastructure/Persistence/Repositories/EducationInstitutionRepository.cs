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

        public async Task<IReadOnlyList<EducationInstitution>> GetByIdsAsync(
            SimulationHostId simulationHostId,
            IReadOnlyCollection<EducationInstitutionId> institutionIds,
            CancellationToken cancellationToken = default)
        {
            if (institutionIds.Count == 0)
                return Array.Empty<EducationInstitution>();

            return await dbContext.Institutions
               .Where(institution => institution.SimulationHostId == simulationHostId
                                     && institutionIds.Contains(institution.Id))
               .ToListAsync(cancellationToken);
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

        public async Task<IReadOnlyList<EducationInstitution>> ListActiveAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.Institutions
               .AsNoTracking()
               .Where(institution => institution.SimulationHostId == simulationHostId
                                     && institution.IsActive)
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

        public Task AddRangeAsync(
            IReadOnlyCollection<EducationInstitution> institutions,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(institutions);
            dbContext.Institutions.AddRange(institutions);
            return Task.CompletedTask;
        }
    }
}
