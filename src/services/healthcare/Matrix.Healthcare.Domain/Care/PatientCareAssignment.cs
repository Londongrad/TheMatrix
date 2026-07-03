using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Care;

public sealed class PatientCareAssignment : AggregateRoot<PatientCareAssignmentId>
{
    private PatientCareAssignment(
        PatientCareAssignmentId id,
        SimulationHostId simulationHostId,
        PatientId patientId,
        CareFacilityId careFacilityId,
        DateOnly careDate,
        CareNeedUrgency urgency,
        long assessmentRevision,
        long lifecycleRevision,
        DateTimeOffset assignedAtUtc)
        : base(id)
    {
        SimulationHostId = simulationHostId;
        PatientId = patientId;
        CareFacilityId = careFacilityId;
        CareDate = careDate;
        Urgency = EnsureUrgency(urgency);
        AssessmentRevision = EnsureRevision(assessmentRevision, nameof(assessmentRevision));
        LifecycleRevision = EnsureRevision(lifecycleRevision, nameof(lifecycleRevision));
        AssignedAtUtc = EnsureUtc(assignedAtUtc);
    }

    private PatientCareAssignment()
        : base(default(PatientCareAssignmentId))
    {
    }

    public PatientCareAssignmentId PatientCareAssignmentId => Id;
    public SimulationHostId SimulationHostId { get; private set; }
    public PatientId PatientId { get; private set; }
    public CareFacilityId CareFacilityId { get; private set; }
    public DateOnly CareDate { get; private set; }
    public CareNeedUrgency Urgency { get; private set; }
    public long AssessmentRevision { get; private set; }
    public long LifecycleRevision { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }

    public static PatientCareAssignment Assign(
        PatientCareAssignmentId id,
        SimulationHostId simulationHostId,
        PatientId patientId,
        CareFacilityId careFacilityId,
        DateOnly careDate,
        CareNeedUrgency urgency,
        long assessmentRevision,
        long lifecycleRevision,
        DateTimeOffset assignedAtUtc)
    {
        return new PatientCareAssignment(
            id,
            simulationHostId,
            patientId,
            careFacilityId,
            careDate,
            urgency,
            assessmentRevision,
            lifecycleRevision,
            assignedAtUtc);
    }

    private static CareNeedUrgency EnsureUrgency(CareNeedUrgency urgency)
    {
        return Enum.IsDefined(urgency)
            ? urgency
            : throw new ArgumentOutOfRangeException(nameof(urgency));
    }

    private static long EnsureRevision(long value, string parameterName)
    {
        return value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(parameterName);
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        return value.Offset == TimeSpan.Zero
            ? value
            : throw new ArgumentException(
                message: "Patient care assignment timestamps must be expressed in UTC.",
                paramName: nameof(value));
    }
}
