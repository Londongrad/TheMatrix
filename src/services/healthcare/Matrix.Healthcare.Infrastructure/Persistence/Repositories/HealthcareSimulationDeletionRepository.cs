using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories
{
    public sealed class HealthcareSimulationDeletionRepository(HealthcareDbContext dbContext)
        : IHealthcareSimulationDeletionRepository
    {
        public async Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            HealthcareSimulationDeletionState? state = await dbContext.SimulationDeletionStates.FindAsync(
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
                await dbContext.PatientMedicalRecords
                   .Where(record => record.SimulationHostId == simulationHostId)
                   .ExecuteDeleteAsync(cancellationToken);
                await dbContext.PatientProfiles
                   .Where(profile => profile.SimulationHostId == simulationHostId)
                   .ExecuteDeleteAsync(cancellationToken);
                return;
            }

            dbContext.PatientMedicalRecords.RemoveRange(
                dbContext.PatientMedicalRecords.Where(record =>
                    record.SimulationHostId == simulationHostId));
            dbContext.PatientProfiles.RemoveRange(
                dbContext.PatientProfiles.Where(profile => profile.SimulationHostId == simulationHostId));
        }

        public async Task RecordAsync(
            SimulationHostId simulationHostId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken = default)
        {
            HealthcareSimulationDeletionState? state = await dbContext.SimulationDeletionStates.FindAsync(
                keyValues: [simulationHostId],
                cancellationToken: cancellationToken);

            if (state is null)
            {
                await dbContext.SimulationDeletionStates.AddAsync(
                    entity: new HealthcareSimulationDeletionState(
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
