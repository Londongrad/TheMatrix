using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Care.AllocatePatientCare;

public sealed class PatientCareAllocator(
    IPatientCareAllocationRepository repository,
    PatientCareAllocationPolicy allocationPolicy)
    : IPatientCareAllocator
{
    public const int MaxAssignmentsPerRun = 10_000;

    public async Task<int> AllocateAsync(
        SimulationHostId simulationHostId,
        DateOnly careDate,
        DateTimeOffset assignedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (assignedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException(
                message: "Patient care allocation timestamps must be expressed in UTC.",
                paramName: nameof(assignedAtUtc));

        IReadOnlyList<CareFacility> facilities = await repository.GetActiveFacilitiesAsync(
            simulationHostId,
            cancellationToken);
        if (facilities.Count == 0)
            return 0;

        CareFacilityId[] facilityIds = facilities
           .Select(facility => facility.CareFacilityId)
           .Distinct()
           .ToArray();
        IReadOnlyList<CareFacilityAssignmentCount> assignmentCounts =
            await repository.GetAssignmentCountsAsync(
                simulationHostId,
                careDate,
                facilityIds,
                cancellationToken);
        Dictionary<CareFacilityId, int> assignedPatientsByFacility = assignmentCounts
           .ToDictionary(count => count.CareFacilityId, count => count.AssignedPatients);
        CareFacilityCapacity[] capacity = facilities
           .Select(facility => new CareFacilityCapacity(
                facility,
                assignedPatientsByFacility.GetValueOrDefault(facility.CareFacilityId)))
           .ToArray();
        long remainingCapacity = capacity.Sum(item => (long)item.RemainingCapacity);
        int candidateLimit = (int)Math.Min(MaxAssignmentsPerRun, remainingCapacity);
        if (candidateLimit == 0)
            return 0;

        IReadOnlyList<PatientCareNeed> careNeeds = await repository.GetUnassignedCareNeedsAsync(
            simulationHostId,
            careDate,
            candidateLimit,
            cancellationToken);
        IReadOnlyList<PatientCareAllocationDecision> decisions = allocationPolicy.Allocate(
            simulationHostId,
            careNeeds,
            capacity);
        if (decisions.Count == 0)
            return 0;

        PatientCareAssignment[] assignments = decisions
           .Select(decision => PatientCareAssignment.Assign(
                id: PatientCareAssignmentId.New(),
                simulationHostId: simulationHostId,
                patientId: decision.PatientId,
                careFacilityId: decision.CareFacilityId,
                careDate: careDate,
                urgency: decision.Urgency,
                assessmentRevision: decision.AssessmentRevision,
                lifecycleRevision: decision.LifecycleRevision,
                assignedAtUtc: assignedAtUtc))
           .ToArray();
        await repository.AddRangeAsync(assignments, cancellationToken);
        return assignments.Length;
    }
}
