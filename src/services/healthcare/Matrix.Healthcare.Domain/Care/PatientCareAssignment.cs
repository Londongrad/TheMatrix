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
        Status = PatientCareAssignmentStatus.Scheduled;
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
    public PatientCareAssignmentStatus Status { get; private set; }
    public DateOnly? ClosedOn { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public PatientCareAssignmentCancellationReason? CancellationReason { get; private set; }
    public int? TreatmentHealthDelta { get; private set; }
    public bool? TreatmentMedicalStateChanged { get; private set; }

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

    public bool TryMarkDelivered(
        DateOnly deliveredOn,
        DateTimeOffset deliveredAtUtc,
        int treatmentHealthDelta,
        bool treatmentMedicalStateChanged)
    {
        DateTimeOffset normalizedTimestamp = EnsureClosure(
            deliveredOn,
            deliveredAtUtc);
        if (Status != PatientCareAssignmentStatus.Scheduled)
            return false;

        Status = PatientCareAssignmentStatus.Delivered;
        ClosedOn = deliveredOn;
        ClosedAtUtc = normalizedTimestamp;
        TreatmentHealthDelta = treatmentHealthDelta;
        TreatmentMedicalStateChanged = treatmentMedicalStateChanged;
        return true;
    }

    public bool TryCancel(
        DateOnly cancelledOn,
        DateTimeOffset cancelledAtUtc,
        PatientCareAssignmentCancellationReason reason)
    {
        DateTimeOffset normalizedTimestamp = EnsureClosure(
            cancelledOn,
            cancelledAtUtc);
        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));
        if (Status != PatientCareAssignmentStatus.Scheduled)
            return false;

        Status = PatientCareAssignmentStatus.Cancelled;
        ClosedOn = cancelledOn;
        ClosedAtUtc = normalizedTimestamp;
        CancellationReason = reason;
        return true;
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

    private DateTimeOffset EnsureClosure(
        DateOnly closedOn,
        DateTimeOffset closedAtUtc)
    {
        DateTimeOffset normalizedTimestamp = EnsureUtc(closedAtUtc);
        if (closedOn < CareDate)
            throw new ArgumentOutOfRangeException(
                paramName: nameof(closedOn),
                message: "A patient care assignment cannot close before its scheduled date.");
        if (normalizedTimestamp < AssignedAtUtc)
            throw new ArgumentOutOfRangeException(
                paramName: nameof(closedAtUtc),
                message: "A patient care assignment cannot close before it was assigned.");

        return normalizedTimestamp;
    }
}
