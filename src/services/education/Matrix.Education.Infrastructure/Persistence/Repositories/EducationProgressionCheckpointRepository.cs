using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Persistence.Repositories
{
    public sealed class EducationProgressionCheckpointRepository(EducationDbContext dbContext)
        : IEducationProgressionCheckpointRepository
    {
        public Task<EducationProgressionCheckpoint?> GetAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            return dbContext.ProgressionCheckpoints.FirstOrDefaultAsync(
                predicate: checkpoint => checkpoint.Id == simulationHostId,
                cancellationToken: cancellationToken);
        }

        public Task AddAsync(
            EducationProgressionCheckpoint checkpoint,
            CancellationToken cancellationToken = default)
        {
            dbContext.ProgressionCheckpoints.Add(checkpoint);
            return Task.CompletedTask;
        }
    }
}
