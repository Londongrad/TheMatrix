using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Care;

public sealed class PatientCareAllocationPolicy
{
    public IReadOnlyList<PatientCareAllocationDecision> Allocate(
        SimulationHostId simulationHostId,
        IReadOnlyCollection<PatientCareNeed> careNeeds,
        IReadOnlyCollection<CareFacilityCapacity> facilityCapacities)
    {
        ArgumentNullException.ThrowIfNull(careNeeds);
        ArgumentNullException.ThrowIfNull(facilityCapacities);

        EnsureSameSimulationHost(
            simulationHostId,
            careNeeds,
            facilityCapacities);

        PatientCareNeed[] candidates = careNeeds
           .Where(careNeed => careNeed.IsActive)
           .DistinctBy(careNeed => careNeed.PatientId)
           .OrderByDescending(careNeed => careNeed.Urgency)
           .ThenBy(careNeed => careNeed.RequestedOn)
           .ThenBy(careNeed => careNeed.PatientId.Value)
           .ToArray();
        if (candidates.Length == 0)
            return [];

        var availableFacilities = new PriorityQueue<FacilityState, FacilityPriority>();
        long totalAvailableCapacity = 0;
        foreach (CareFacilityCapacity capacity in facilityCapacities)
        {
            if (!capacity.Facility.IsActive || capacity.RemainingCapacity == 0)
                continue;

            var state = new FacilityState(
                capacity.Facility.CareFacilityId,
                capacity.Facility.DailyPatientCapacity,
                capacity.AssignedPatients);
            availableFacilities.Enqueue(state, state.GetPriority());
            totalAvailableCapacity += capacity.RemainingCapacity;
        }

        var decisions = new List<PatientCareAllocationDecision>(
            (int)Math.Min(candidates.Length, totalAvailableCapacity));
        foreach (PatientCareNeed careNeed in candidates)
        {
            if (!availableFacilities.TryDequeue(out FacilityState? facility, out _))
                break;

            decisions.Add(new PatientCareAllocationDecision(
                careNeed.PatientId,
                facility.CareFacilityId,
                careNeed.Urgency,
                careNeed.LastAssessmentRevision,
                careNeed.LastLifecycleRevision));

            facility.AssignPatient();
            if (facility.HasRemainingCapacity)
                availableFacilities.Enqueue(facility, facility.GetPriority());
        }

        return decisions;
    }

    private static void EnsureSameSimulationHost(
        SimulationHostId simulationHostId,
        IReadOnlyCollection<PatientCareNeed> careNeeds,
        IReadOnlyCollection<CareFacilityCapacity> facilityCapacities)
    {
        if (careNeeds.Any(careNeed => careNeed.SimulationHostId != simulationHostId))
            throw new InvalidOperationException(
                "Patient care allocation cannot include needs from another simulation host.");
        if (facilityCapacities.Any(capacity =>
                capacity.Facility.SimulationHostId != simulationHostId))
            throw new InvalidOperationException(
                "Patient care allocation cannot include facilities from another simulation host.");
    }

    private sealed class FacilityState(
        CareFacilityId careFacilityId,
        int capacity,
        int assignedPatients)
    {
        internal CareFacilityId CareFacilityId { get; } = careFacilityId;
        internal int Capacity { get; } = capacity;
        internal int AssignedPatients { get; private set; } = assignedPatients;
        internal bool HasRemainingCapacity => AssignedPatients < Capacity;

        internal void AssignPatient()
        {
            if (!HasRemainingCapacity)
                throw new InvalidOperationException("A full care facility cannot accept a patient.");

            AssignedPatients++;
        }

        internal FacilityPriority GetPriority() => new(
            AssignedPatients,
            Capacity,
            CareFacilityId.Value);
    }

    private readonly record struct FacilityPriority(
        int AssignedPatients,
        int Capacity,
        Guid CareFacilityId) : IComparable<FacilityPriority>
    {
        public int CompareTo(FacilityPriority other)
        {
            long leftUtilization = (long)AssignedPatients * other.Capacity;
            long rightUtilization = (long)other.AssignedPatients * Capacity;
            int utilizationComparison = leftUtilization.CompareTo(rightUtilization);
            return utilizationComparison != 0
                ? utilizationComparison
                : CareFacilityId.CompareTo(other.CareFacilityId);
        }
    }
}
