using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Persistence.Repositories
{
    public sealed class EducationSimulationDeletionRepository(EducationDbContext dbContext)
        : IEducationSimulationDeletionRepository
    {
        public async Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            EducationSimulationDeletionState? state = await dbContext.SimulationDeletionStates.FindAsync(
                keyValues: [simulationHostId],
                cancellationToken: cancellationToken);

            return state?.DeletedAtUtc;
        }

        public async Task DeleteSimulationDataAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            if (dbContext.Database.IsRelational())
            {
                await dbContext.Enrollments
                   .Where(enrollment => enrollment.SimulationHostId == simulationHostId)
                   .ExecuteDeleteAsync(cancellationToken);
                await dbContext.Institutions
                   .Where(institution => institution.SimulationHostId == simulationHostId)
                   .ExecuteDeleteAsync(cancellationToken);
                await dbContext.StudentProfiles
                   .Where(profile => profile.SimulationHostId == simulationHostId)
                   .ExecuteDeleteAsync(cancellationToken);
                await dbContext.ProgressionCheckpoints
                   .Where(checkpoint => checkpoint.Id == simulationHostId)
                   .ExecuteDeleteAsync(cancellationToken);
                return;
            }

            dbContext.Enrollments.RemoveRange(
                dbContext.Enrollments.Where(enrollment => enrollment.SimulationHostId == simulationHostId));
            dbContext.Institutions.RemoveRange(
                dbContext.Institutions.Where(institution => institution.SimulationHostId == simulationHostId));
            dbContext.StudentProfiles.RemoveRange(
                dbContext.StudentProfiles.Where(profile => profile.SimulationHostId == simulationHostId));
            dbContext.ProgressionCheckpoints.RemoveRange(
                dbContext.ProgressionCheckpoints.Where(checkpoint => checkpoint.Id == simulationHostId));
        }

        public async Task RecordAsync(
            SimulationHostId simulationHostId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken = default)
        {
            EducationSimulationDeletionState? state = await dbContext.SimulationDeletionStates.FindAsync(
                keyValues: [simulationHostId],
                cancellationToken: cancellationToken);

            if (state is null)
            {
                await dbContext.SimulationDeletionStates.AddAsync(
                    entity: new EducationSimulationDeletionState(
                        simulationHostId: simulationHostId,
                        deletedAtUtc: deletedAtUtc,
                        updatedAtUtc: updatedAtUtc),
                    cancellationToken: cancellationToken);
                return;
            }

            state.Record(
                deletedAtUtc: deletedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }
    }
}
