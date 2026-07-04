using Matrix.Healthcare.Application.Care.AllocatePatientCare;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions;

public interface IPatientCareAllocationRepository
{
    Task<IReadOnlyList<CareFacility>> GetActiveFacilitiesAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CareFacilityAssignmentCount>> GetAssignmentCountsAsync(
        SimulationHostId simulationHostId,
        DateOnly careDate,
        IReadOnlyCollection<CareFacilityId> careFacilityIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientCareNeed>> GetUnassignedCareNeedsAsync(
        SimulationHostId simulationHostId,
        DateOnly careDate,
        int maximumCount,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<PatientCareAssignment> assignments,
        CancellationToken cancellationToken = default);
}
