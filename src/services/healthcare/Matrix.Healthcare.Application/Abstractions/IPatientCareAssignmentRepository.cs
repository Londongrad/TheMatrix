using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions;

public interface IPatientCareAssignmentRepository
{
    Task<IReadOnlyList<PatientCareAssignment>> GetDueScheduledByPatientIdsAsync(
        SimulationHostId simulationHostId,
        IReadOnlyCollection<PatientId> patientIds,
        DateOnly dueThroughDate,
        CancellationToken cancellationToken = default);
}
