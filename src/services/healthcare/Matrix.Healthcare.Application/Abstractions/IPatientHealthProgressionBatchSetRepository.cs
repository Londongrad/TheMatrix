using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions;

public interface IPatientHealthProgressionBatchSetRepository
{
    Task<PatientHealthProgressionBatchSet?> GetAsync(
        SimulationHostId simulationHostId,
        long sourceRevision,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PatientHealthProgressionBatchSet batchSet,
        CancellationToken cancellationToken = default);
}
