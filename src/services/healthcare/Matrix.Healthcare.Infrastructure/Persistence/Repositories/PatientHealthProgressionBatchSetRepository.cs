using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories;

public sealed class PatientHealthProgressionBatchSetRepository(HealthcareDbContext dbContext)
    : IPatientHealthProgressionBatchSetRepository
{
    public Task<PatientHealthProgressionBatchSet?> GetAsync(
        SimulationHostId simulationHostId,
        long sourceRevision,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PatientHealthProgressionBatchSets.SingleOrDefaultAsync(
            batchSet => batchSet.SimulationHostId == simulationHostId
                        && batchSet.SourceRevision == sourceRevision,
            cancellationToken);
    }

    public async Task AddAsync(
        PatientHealthProgressionBatchSet batchSet,
        CancellationToken cancellationToken = default)
    {
        await dbContext.PatientHealthProgressionBatchSets.AddAsync(
            batchSet,
            cancellationToken);
    }
}
