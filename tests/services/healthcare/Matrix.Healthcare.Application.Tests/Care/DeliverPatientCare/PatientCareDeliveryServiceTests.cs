using Matrix.Healthcare.Application.Care.DeliverPatientCare;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Care.DeliverPatientCare;

public sealed class PatientCareDeliveryServiceTests
{
    private static readonly SimulationHostId HostId = new(Guid.NewGuid());
    private static readonly PatientId PatientId = new(Guid.NewGuid());
    private static readonly CareFacilityId FacilityId = new(Guid.NewGuid());
    private static readonly DateOnly CareDate = new(2048, 5, 7);
    private static readonly DateTimeOffset DeliveredAtUtc =
        DateTimeOffset.Parse("2048-05-07T10:00:00+00:00");
    private readonly PatientCareDeliveryService _service = new(
        new PatientCareTreatmentPolicy());

    [Fact]
    public void Deliver_ValidAssignment_AppliesTreatmentAndCompletesAssignment()
    {
        PatientCareAssignment assignment = CreateAssignment();
        PatientMedicalRecord record = CreateMedicalRecord();

        PatientCareDeliveryResult result = _service.Deliver(
            assignment,
            HostId,
            patientLifecycleRevision: 2,
            record,
            CreateCareNeed(),
            CreateFacility(),
            CareDate,
            DeliveredAtUtc);

        Assert.True(result.Delivered);
        Assert.False(result.Cancelled);
        Assert.NotNull(result.TreatmentOutcome);
        Assert.Equal(6, result.TreatmentOutcome.HealthDelta);
        Assert.Equal(IllnessSeverity.Moderate, record.Illness.CurrentSeverity);
        Assert.Equal(PatientCareAssignmentStatus.Delivered, assignment.Status);
        Assert.Equal(6, assignment.TreatmentHealthDelta);
    }

    [Fact]
    public void Deliver_ChangedLifecycle_CancelsWithoutTreatingPatient()
    {
        PatientCareAssignment assignment = CreateAssignment();
        PatientMedicalRecord record = CreateMedicalRecord();

        PatientCareDeliveryResult result = _service.Deliver(
            assignment,
            HostId,
            patientLifecycleRevision: 3,
            record,
            CreateCareNeed(),
            CreateFacility(),
            CareDate,
            DeliveredAtUtc);

        Assert.True(result.Cancelled);
        Assert.Equal(
            PatientCareAssignmentCancellationReason.PatientLifecycleChanged,
            assignment.CancellationReason);
        Assert.Equal(50, record.Health.Value);
        Assert.Equal(IllnessSeverity.Severe, record.Illness.CurrentSeverity);
    }

    [Fact]
    public void Deliver_InactiveCareNeed_CancelsAsNoLongerRequired()
    {
        PatientCareNeed careNeed = CreateCareNeed();
        careNeed.TrySynchronizeAssessment(
            HostId,
            urgency: null,
            assessmentDate: CareDate,
            assessmentRevision: 18,
            lifecycleRevision: 2,
            assessedAtUtc: DeliveredAtUtc);
        PatientCareAssignment assignment = CreateAssignment();

        PatientCareDeliveryResult result = _service.Deliver(
            assignment,
            HostId,
            patientLifecycleRevision: 2,
            CreateMedicalRecord(),
            careNeed,
            CreateFacility(),
            CareDate,
            DeliveredAtUtc);

        Assert.True(result.Cancelled);
        Assert.Equal(
            PatientCareAssignmentCancellationReason.CareNoLongerRequired,
            assignment.CancellationReason);
    }

    [Fact]
    public void Deliver_InactiveFacility_CancelsAsUnavailable()
    {
        PatientCareAssignment assignment = CreateAssignment();

        PatientCareDeliveryResult result = _service.Deliver(
            assignment,
            HostId,
            patientLifecycleRevision: 2,
            CreateMedicalRecord(),
            CreateCareNeed(),
            CreateFacility(isActive: false),
            CareDate,
            DeliveredAtUtc);

        Assert.True(result.Cancelled);
        Assert.Equal(
            PatientCareAssignmentCancellationReason.FacilityUnavailable,
            assignment.CancellationReason);
    }

    [Fact]
    public void Deliver_DegradedOperations_AuditsReducedTreatmentEffect()
    {
        PatientCareAssignment assignment = CreateAssignment();
        PatientMedicalRecord record = CreateMedicalRecord();
        var profile = new CareOperationalProfile(
            new CareQualityMultiplier(0.45m),
            CareAvailabilityIndex.None,
            CareAvailabilityIndex.Full);

        PatientCareDeliveryResult result = _service.Deliver(
            assignment,
            HostId,
            patientLifecycleRevision: 2,
            record,
            CreateCareNeed(),
            CreateFacility(),
            CareDate,
            DeliveredAtUtc,
            profile);

        Assert.True(result.Delivered);
        Assert.Equal(0, assignment.TreatmentHealthDelta);
        Assert.False(assignment.TreatmentMedicalStateChanged);
        Assert.Equal(50, record.Health.Value);
        Assert.Equal(IllnessSeverity.Severe, record.Illness.CurrentSeverity);
    }

    private static PatientCareAssignment CreateAssignment()
    {
        return PatientCareAssignment.Assign(
            PatientCareAssignmentId.New(),
            HostId,
            PatientId,
            FacilityId,
            CareDate,
            CareNeedUrgency.Acute,
            assessmentRevision: 17,
            lifecycleRevision: 2,
            assignedAtUtc: DeliveredAtUtc.AddDays(-1));
    }

    private static PatientMedicalRecord CreateMedicalRecord()
    {
        return PatientMedicalRecord.Register(
            PatientId,
            HostId,
            new HealthScore(50),
            PatientIllnessState.Active(
                IllnessKind.Infection,
                IllnessSeverity.Severe,
                CareDate.AddDays(-3)),
            lifecycleRevision: 2);
    }

    private static PatientCareNeed CreateCareNeed()
    {
        return PatientCareNeed.Register(
            PatientId,
            HostId,
            CareNeedUrgency.Acute,
            requestedOn: CareDate.AddDays(-1),
            assessmentRevision: 17,
            lifecycleRevision: 2,
            assessedAtUtc: DeliveredAtUtc.AddDays(-1));
    }

    private static CareFacility CreateFacility(bool isActive = true)
    {
        return CareFacility.Register(
            FacilityId,
            HostId,
            "Central Hospital",
            new CareFacilityKindKey("Hospital"),
            locationAnchorId: null,
            dailyPatientCapacity: 20,
            isActive: isActive,
            sourceRevision: 7,
            synchronizedAtUtc: DeliveredAtUtc.AddDays(-1));
    }
}
