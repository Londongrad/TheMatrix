using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Care;

public sealed class PatientCareNeed : AggregateRoot<PatientId>
{
    private PatientCareNeed(
        PatientId patientId,
        SimulationHostId simulationHostId,
        CareNeedUrgency urgency,
        DateOnly requestedOn,
        long assessmentRevision,
        long lifecycleRevision,
        DateTimeOffset assessedAtUtc)
        : base(patientId)
    {
        SimulationHostId = simulationHostId;
        Urgency = EnsureUrgency(urgency);
        IsActive = true;
        RequestedOn = requestedOn;
        LastAssessmentRevision = EnsureRevision(assessmentRevision, nameof(assessmentRevision));
        LastLifecycleRevision = EnsureRevision(lifecycleRevision, nameof(lifecycleRevision));
        LastAssessedAtUtc = EnsureUtc(assessedAtUtc);
    }

    private PatientCareNeed()
        : base(default(PatientId))
    {
    }

    public PatientId PatientId => Id;
    public SimulationHostId SimulationHostId { get; private set; }
    public CareNeedUrgency Urgency { get; private set; }
    public bool IsActive { get; private set; }
    public DateOnly RequestedOn { get; private set; }
    public DateOnly? ResolvedOn { get; private set; }
    public long LastAssessmentRevision { get; private set; }
    public long LastLifecycleRevision { get; private set; }
    public DateTimeOffset LastAssessedAtUtc { get; private set; }

    public static PatientCareNeed Register(
        PatientId patientId,
        SimulationHostId simulationHostId,
        CareNeedUrgency urgency,
        DateOnly requestedOn,
        long assessmentRevision,
        long lifecycleRevision,
        DateTimeOffset assessedAtUtc)
    {
        return new PatientCareNeed(
            patientId,
            simulationHostId,
            urgency,
            requestedOn,
            assessmentRevision,
            lifecycleRevision,
            assessedAtUtc);
    }

    public bool TrySynchronizeAssessment(
        SimulationHostId simulationHostId,
        CareNeedUrgency? urgency,
        DateOnly assessmentDate,
        long assessmentRevision,
        long lifecycleRevision,
        DateTimeOffset assessedAtUtc)
    {
        EnsureSameSimulationHost(simulationHostId);
        long normalizedAssessmentRevision = EnsureRevision(
            assessmentRevision,
            nameof(assessmentRevision));
        long normalizedLifecycleRevision = EnsureRevision(
            lifecycleRevision,
            nameof(lifecycleRevision));
        DateTimeOffset normalizedAssessedAtUtc = EnsureUtc(assessedAtUtc);

        if (normalizedLifecycleRevision < LastLifecycleRevision
            || (normalizedLifecycleRevision == LastLifecycleRevision
                && normalizedAssessmentRevision <= LastAssessmentRevision))
            return false;

        if (urgency.HasValue)
        {
            CareNeedUrgency normalizedUrgency = EnsureUrgency(urgency.Value);
            if (!IsActive)
                RequestedOn = assessmentDate;

            Urgency = normalizedUrgency;
            IsActive = true;
            ResolvedOn = null;
        }
        else if (IsActive)
        {
            IsActive = false;
            ResolvedOn = assessmentDate;
        }

        LastAssessmentRevision = normalizedAssessmentRevision;
        LastLifecycleRevision = normalizedLifecycleRevision;
        LastAssessedAtUtc = normalizedAssessedAtUtc;
        return true;
    }

    private void EnsureSameSimulationHost(SimulationHostId simulationHostId)
    {
        if (simulationHostId != SimulationHostId)
            throw new InvalidOperationException(
                "A patient care need cannot move between simulation hosts.");
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
                message: "Care need assessment timestamps must be expressed in UTC.",
                paramName: nameof(value));
    }
}
