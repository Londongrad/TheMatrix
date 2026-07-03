using Matrix.Healthcare.Domain.Facilities;

namespace Matrix.Healthcare.Domain.Care;

public sealed record CareFacilityCapacity
{
    public CareFacilityCapacity(CareFacility facility, int assignedPatients)
    {
        Facility = facility ?? throw new ArgumentNullException(nameof(facility));
        AssignedPatients = assignedPatients >= 0
            ? assignedPatients
            : throw new ArgumentOutOfRangeException(nameof(assignedPatients));
    }

    public CareFacility Facility { get; }
    public int AssignedPatients { get; }
    public int RemainingCapacity => Math.Max(
        0,
        Facility.DailyPatientCapacity - AssignedPatients);
}
