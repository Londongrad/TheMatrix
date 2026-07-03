using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Care;

public sealed class PatientCareAllocationPolicyTests
{
    private static readonly SimulationHostId SimulationHostId = new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly DateTimeOffset AssessedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");
    private readonly PatientCareAllocationPolicy _policy = new();

    [Fact]
    public void Allocate_WhenCapacityIsScarce_PrioritizesUrgencyBeforeWaitTime()
    {
        PatientCareNeed routine = CreateCareNeed(
            "20000000-0000-0000-0000-000000000001",
            CareNeedUrgency.Routine,
            requestedOn: new DateOnly(2048, 5, 1));
        PatientCareNeed emergency = CreateCareNeed(
            "20000000-0000-0000-0000-000000000002",
            CareNeedUrgency.Emergency,
            requestedOn: new DateOnly(2048, 5, 6));

        IReadOnlyList<PatientCareAllocationDecision> decisions = _policy.Allocate(
            SimulationHostId,
            [routine, emergency],
            [new CareFacilityCapacity(CreateFacility(capacity: 1), assignedPatients: 0)]);

        PatientCareAllocationDecision decision = Assert.Single(decisions);
        Assert.Equal(emergency.PatientId, decision.PatientId);
        Assert.Equal(CareNeedUrgency.Emergency, decision.Urgency);
        Assert.Equal(emergency.LastAssessmentRevision, decision.AssessmentRevision);
    }

    [Fact]
    public void Allocate_MultipleFacilities_BalancesNormalizedUtilizationDeterministically()
    {
        CareFacility first = CreateFacility(
            capacity: 2,
            id: "30000000-0000-0000-0000-000000000001");
        CareFacility second = CreateFacility(
            capacity: 2,
            id: "30000000-0000-0000-0000-000000000002");
        PatientCareNeed[] needs =
        [
            CreateCareNeed("20000000-0000-0000-0000-000000000001"),
            CreateCareNeed("20000000-0000-0000-0000-000000000002"),
            CreateCareNeed("20000000-0000-0000-0000-000000000003")
        ];

        IReadOnlyList<PatientCareAllocationDecision> decisions = _policy.Allocate(
            SimulationHostId,
            needs,
            [
                new CareFacilityCapacity(first, assignedPatients: 0),
                new CareFacilityCapacity(second, assignedPatients: 0)
            ]);

        Assert.Equal(3, decisions.Count);
        Assert.Equal(first.CareFacilityId, decisions[0].CareFacilityId);
        Assert.Equal(second.CareFacilityId, decisions[1].CareFacilityId);
        Assert.Equal(first.CareFacilityId, decisions[2].CareFacilityId);
    }

    [Fact]
    public void Allocate_InactiveAndFullFacilities_DoNotReceivePatients()
    {
        CareFacility full = CreateFacility(
            capacity: 1,
            id: "30000000-0000-0000-0000-000000000001");
        CareFacility inactive = CreateFacility(
            capacity: 10,
            id: "30000000-0000-0000-0000-000000000002",
            isActive: false);

        IReadOnlyList<PatientCareAllocationDecision> decisions = _policy.Allocate(
            SimulationHostId,
            [CreateCareNeed("20000000-0000-0000-0000-000000000001")],
            [
                new CareFacilityCapacity(full, assignedPatients: 1),
                new CareFacilityCapacity(inactive, assignedPatients: 0)
            ]);

        Assert.Empty(decisions);
    }

    [Fact]
    public void Allocate_InactiveCareNeed_DoesNotConsumeCapacity()
    {
        PatientCareNeed resolved = CreateCareNeed(
            "20000000-0000-0000-0000-000000000001");
        resolved.TrySynchronizeAssessment(
            SimulationHostId,
            urgency: null,
            assessmentDate: new DateOnly(2048, 5, 7),
            assessmentRevision: 18,
            lifecycleRevision: 0,
            assessedAtUtc: AssessedAtUtc.AddDays(1));

        IReadOnlyList<PatientCareAllocationDecision> decisions = _policy.Allocate(
            SimulationHostId,
            [resolved],
            [new CareFacilityCapacity(CreateFacility(capacity: 1), assignedPatients: 0)]);

        Assert.Empty(decisions);
    }

    [Fact]
    public void Allocate_DataFromAnotherHost_ThrowsInvalidOperationException()
    {
        PatientCareNeed foreignNeed = CreateCareNeed(
            "20000000-0000-0000-0000-000000000001",
            simulationHostId: new SimulationHostId(
                Guid.Parse("10000000-0000-0000-0000-000000000002")));

        Assert.Throws<InvalidOperationException>(() => _policy.Allocate(
            SimulationHostId,
            [foreignNeed],
            [new CareFacilityCapacity(CreateFacility(capacity: 1), assignedPatients: 0)]));
    }

    private static PatientCareNeed CreateCareNeed(
        string patientId,
        CareNeedUrgency urgency = CareNeedUrgency.Urgent,
        DateOnly? requestedOn = null,
        SimulationHostId? simulationHostId = null)
    {
        return PatientCareNeed.Register(
            patientId: new PatientId(Guid.Parse(patientId)),
            simulationHostId: simulationHostId ?? SimulationHostId,
            urgency: urgency,
            requestedOn: requestedOn ?? new DateOnly(2048, 5, 6),
            assessmentRevision: 17,
            lifecycleRevision: 0,
            assessedAtUtc: AssessedAtUtc);
    }

    private static CareFacility CreateFacility(
        int capacity,
        string id = "30000000-0000-0000-0000-000000000001",
        bool isActive = true)
    {
        return CareFacility.Register(
            id: new CareFacilityId(Guid.Parse(id)),
            simulationHostId: SimulationHostId,
            name: "Central Hospital",
            kind: new CareFacilityKindKey("Hospital"),
            locationAnchorId: null,
            dailyPatientCapacity: capacity,
            isActive: isActive,
            sourceRevision: 7,
            synchronizedAtUtc: AssessedAtUtc);
    }
}
