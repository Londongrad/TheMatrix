using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Care;

public sealed class PatientCareAssignmentTests
{
    private static readonly PatientCareAssignmentId AssignmentId = new(Guid.NewGuid());
    private static readonly SimulationHostId SimulationHostId = new(Guid.NewGuid());
    private static readonly PatientId PatientId = new(Guid.NewGuid());
    private static readonly CareFacilityId CareFacilityId = new(Guid.NewGuid());
    private static readonly DateOnly CareDate = new(2048, 5, 6);
    private static readonly DateTimeOffset AssignedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public void Assign_PreservesCareNeedAndCapacityFacts()
    {
        PatientCareAssignment assignment = CreateAssignment();

        Assert.Equal(AssignmentId, assignment.PatientCareAssignmentId);
        Assert.Equal(SimulationHostId, assignment.SimulationHostId);
        Assert.Equal(PatientId, assignment.PatientId);
        Assert.Equal(CareFacilityId, assignment.CareFacilityId);
        Assert.Equal(CareDate, assignment.CareDate);
        Assert.Equal(CareNeedUrgency.Emergency, assignment.Urgency);
        Assert.Equal(17, assignment.AssessmentRevision);
        Assert.Equal(2, assignment.LifecycleRevision);
        Assert.Equal(AssignedAtUtc, assignment.AssignedAtUtc);
        Assert.Equal(PatientCareAssignmentStatus.Scheduled, assignment.Status);
        Assert.Null(assignment.ClosedOn);
        Assert.Null(assignment.ClosedAtUtc);
        Assert.Null(assignment.CancellationReason);
        Assert.Null(assignment.TreatmentHealthDelta);
        Assert.Null(assignment.TreatmentMedicalStateChanged);
    }

    [Fact]
    public void TryMarkDelivered_ScheduledAssignment_RecordsTreatmentAudit()
    {
        PatientCareAssignment assignment = CreateAssignment();
        DateTimeOffset deliveredAtUtc = AssignedAtUtc.AddHours(2);

        bool delivered = assignment.TryMarkDelivered(
            CareDate,
            deliveredAtUtc,
            treatmentHealthDelta: 7,
            treatmentMedicalStateChanged: true);

        Assert.True(delivered);
        Assert.Equal(PatientCareAssignmentStatus.Delivered, assignment.Status);
        Assert.Equal(CareDate, assignment.ClosedOn);
        Assert.Equal(deliveredAtUtc, assignment.ClosedAtUtc);
        Assert.Equal(7, assignment.TreatmentHealthDelta);
        Assert.True(assignment.TreatmentMedicalStateChanged);
        Assert.Null(assignment.CancellationReason);
    }

    [Fact]
    public void TryCancel_ScheduledAssignment_RecordsReasonAndCannotCloseTwice()
    {
        PatientCareAssignment assignment = CreateAssignment();

        bool cancelled = assignment.TryCancel(
            CareDate,
            AssignedAtUtc.AddHours(1),
            PatientCareAssignmentCancellationReason.FacilityUnavailable);
        bool delivered = assignment.TryMarkDelivered(
            CareDate,
            AssignedAtUtc.AddHours(2),
            treatmentHealthDelta: 5,
            treatmentMedicalStateChanged: true);

        Assert.True(cancelled);
        Assert.False(delivered);
        Assert.Equal(PatientCareAssignmentStatus.Cancelled, assignment.Status);
        Assert.Equal(
            PatientCareAssignmentCancellationReason.FacilityUnavailable,
            assignment.CancellationReason);
        Assert.Null(assignment.TreatmentHealthDelta);
        Assert.Null(assignment.TreatmentMedicalStateChanged);
    }

    [Fact]
    public void TryMarkDelivered_BeforeScheduledDate_ThrowsArgumentOutOfRangeException()
    {
        PatientCareAssignment assignment = CreateAssignment();

        Assert.Throws<ArgumentOutOfRangeException>(() => assignment.TryMarkDelivered(
            CareDate.AddDays(-1),
            AssignedAtUtc.AddHours(1),
            treatmentHealthDelta: 1,
            treatmentMedicalStateChanged: false));
    }

    [Fact]
    public void AssignmentId_WhenEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new PatientCareAssignmentId(Guid.Empty));
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(17, -1)]
    public void Assign_WhenRevisionIsNegative_ThrowsArgumentOutOfRangeException(
        long assessmentRevision,
        long lifecycleRevision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PatientCareAssignment.Assign(
            AssignmentId,
            SimulationHostId,
            PatientId,
            CareFacilityId,
            CareDate,
            CareNeedUrgency.Emergency,
            assessmentRevision,
            lifecycleRevision,
            AssignedAtUtc));
    }

    [Fact]
    public void Assign_WhenTimestampIsNotUtc_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PatientCareAssignment.Assign(
            AssignmentId,
            SimulationHostId,
            PatientId,
            CareFacilityId,
            CareDate,
            CareNeedUrgency.Emergency,
            assessmentRevision: 17,
            lifecycleRevision: 2,
            assignedAtUtc: AssignedAtUtc.ToOffset(TimeSpan.FromHours(3))));
    }

    private static PatientCareAssignment CreateAssignment()
    {
        return PatientCareAssignment.Assign(
            AssignmentId,
            SimulationHostId,
            PatientId,
            CareFacilityId,
            CareDate,
            CareNeedUrgency.Emergency,
            assessmentRevision: 17,
            lifecycleRevision: 2,
            assignedAtUtc: AssignedAtUtc);
    }
}
