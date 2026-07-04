using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories;

public sealed class PatientCareAllocationRepositoryTests
{
    private static readonly SimulationHostId SimulationHostId = new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly SimulationHostId ForeignHostId = new(
        Guid.Parse("10000000-0000-0000-0000-000000000002"));
    private static readonly DateOnly CareDate = new(2048, 5, 6);
    private static readonly DateTimeOffset AssignedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task GetActiveFacilities_ReturnsOnlyTargetHostCapacity()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        CareFacility first = CreateFacility(
            SimulationHostId,
            "30000000-0000-0000-0000-000000000001");
        CareFacility second = CreateFacility(
            SimulationHostId,
            "30000000-0000-0000-0000-000000000002");
        CareFacility inactive = CreateFacility(
            SimulationHostId,
            "30000000-0000-0000-0000-000000000003",
            isActive: false);
        CareFacility foreign = CreateFacility(
            ForeignHostId,
            "30000000-0000-0000-0000-000000000004");
        dbContext.CareFacilities.AddRange(second, inactive, foreign, first);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new PatientCareAllocationRepository(dbContext);

        IReadOnlyList<CareFacility> facilities =
            await repository.GetActiveFacilitiesAsync(SimulationHostId);

        Assert.Equal(2, facilities.Count);
        Assert.Contains(facilities, facility => facility.CareFacilityId == first.CareFacilityId);
        Assert.Contains(facilities, facility => facility.CareFacilityId == second.CareFacilityId);
        Assert.All(facilities, facility => Assert.Equal(
            EntityState.Detached,
            dbContext.Entry(facility).State));
    }

    [Fact]
    public async Task GetAssignmentCounts_GroupsOnlyRequestedDailyCapacityUsage()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        CareFacility first = CreateFacility(
            SimulationHostId,
            "30000000-0000-0000-0000-000000000001");
        CareFacility second = CreateFacility(
            SimulationHostId,
            "30000000-0000-0000-0000-000000000002");
        CareFacility foreign = CreateFacility(
            ForeignHostId,
            "30000000-0000-0000-0000-000000000003");
        dbContext.CareFacilities.AddRange(first, second, foreign);
        dbContext.PatientCareAssignments.AddRange(
            CreateAssignment(SimulationHostId, first.CareFacilityId, Guid.NewGuid(), CareDate),
            CreateAssignment(SimulationHostId, first.CareFacilityId, Guid.NewGuid(), CareDate),
            CreateAssignment(SimulationHostId, second.CareFacilityId, Guid.NewGuid(), CareDate.AddDays(1)),
            CreateAssignment(ForeignHostId, foreign.CareFacilityId, Guid.NewGuid(), CareDate));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new PatientCareAllocationRepository(dbContext);

        var counts = await repository.GetAssignmentCountsAsync(
            SimulationHostId,
            CareDate,
            [first.CareFacilityId, second.CareFacilityId]);

        var count = Assert.Single(counts);
        Assert.Equal(first.CareFacilityId, count.CareFacilityId);
        Assert.Equal(2, count.AssignedPatients);
    }

    [Fact]
    public async Task GetUnassignedCareNeeds_FiltersAndOrdersCandidatesInDatabase()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        CareFacility facility = CreateFacility(
            SimulationHostId,
            "30000000-0000-0000-0000-000000000001");
        PatientCareNeed emergency = CreateCareNeed(
            SimulationHostId,
            "20000000-0000-0000-0000-000000000001",
            CareNeedUrgency.Emergency,
            CareDate);
        PatientCareNeed assignedEmergency = CreateCareNeed(
            SimulationHostId,
            "20000000-0000-0000-0000-000000000002",
            CareNeedUrgency.Emergency,
            CareDate.AddDays(-1));
        PatientCareNeed oldestUrgent = CreateCareNeed(
            SimulationHostId,
            "20000000-0000-0000-0000-000000000003",
            CareNeedUrgency.Urgent,
            CareDate.AddDays(-2));
        PatientCareNeed newerUrgent = CreateCareNeed(
            SimulationHostId,
            "20000000-0000-0000-0000-000000000004",
            CareNeedUrgency.Urgent,
            CareDate.AddDays(-1));
        PatientCareNeed resolved = CreateCareNeed(
            SimulationHostId,
            "20000000-0000-0000-0000-000000000005",
            CareNeedUrgency.Acute,
            CareDate);
        resolved.TrySynchronizeAssessment(
            SimulationHostId,
            urgency: null,
            assessmentDate: CareDate,
            assessmentRevision: 18,
            lifecycleRevision: 0,
            assessedAtUtc: AssignedAtUtc.AddSeconds(1));
        PatientCareNeed foreign = CreateCareNeed(
            ForeignHostId,
            "20000000-0000-0000-0000-000000000006",
            CareNeedUrgency.Emergency,
            CareDate);
        dbContext.CareFacilities.Add(facility);
        dbContext.PatientCareNeeds.AddRange(
            newerUrgent,
            resolved,
            assignedEmergency,
            foreign,
            oldestUrgent,
            emergency);
        dbContext.PatientCareAssignments.Add(CreateAssignment(
            SimulationHostId,
            facility.CareFacilityId,
            assignedEmergency.PatientId.Value,
            CareDate));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new PatientCareAllocationRepository(dbContext);

        IReadOnlyList<PatientCareNeed> candidates =
            await repository.GetUnassignedCareNeedsAsync(
                SimulationHostId,
                CareDate,
                maximumCount: 2);

        Assert.Equal(
            [emergency.PatientId, oldestUrgent.PatientId],
            candidates.Select(careNeed => careNeed.PatientId));
        Assert.All(candidates, careNeed => Assert.Equal(
            EntityState.Detached,
            dbContext.Entry(careNeed).State));
    }

    [Fact]
    public async Task AddRange_PersistsCareAssignments()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        CareFacility facility = CreateFacility(
            SimulationHostId,
            "30000000-0000-0000-0000-000000000001");
        PatientCareAssignment assignment = CreateAssignment(
            SimulationHostId,
            facility.CareFacilityId,
            Guid.NewGuid(),
            CareDate);
        dbContext.CareFacilities.Add(facility);
        var repository = new PatientCareAllocationRepository(dbContext);

        await repository.AddRangeAsync([assignment]);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        PatientCareAssignment persisted = Assert.Single(
            await dbContext.PatientCareAssignments.ToListAsync());
        Assert.Equal(assignment.PatientCareAssignmentId, persisted.PatientCareAssignmentId);
        Assert.Equal(assignment.PatientId, persisted.PatientId);
        Assert.Equal(facility.CareFacilityId, persisted.CareFacilityId);
    }

    private static CareFacility CreateFacility(
        SimulationHostId simulationHostId,
        string facilityId,
        bool isActive = true)
    {
        return CareFacility.Register(
            id: new CareFacilityId(Guid.Parse(facilityId)),
            simulationHostId: simulationHostId,
            name: "Central Hospital",
            kind: new CareFacilityKindKey("Hospital"),
            locationAnchorId: null,
            dailyPatientCapacity: 10,
            isActive: isActive,
            sourceRevision: 7,
            synchronizedAtUtc: AssignedAtUtc);
    }

    private static PatientCareNeed CreateCareNeed(
        SimulationHostId simulationHostId,
        string patientId,
        CareNeedUrgency urgency,
        DateOnly requestedOn)
    {
        return PatientCareNeed.Register(
            patientId: new PatientId(Guid.Parse(patientId)),
            simulationHostId: simulationHostId,
            urgency: urgency,
            requestedOn: requestedOn,
            assessmentRevision: 17,
            lifecycleRevision: 0,
            assessedAtUtc: AssignedAtUtc);
    }

    private static PatientCareAssignment CreateAssignment(
        SimulationHostId simulationHostId,
        CareFacilityId careFacilityId,
        Guid patientId,
        DateOnly careDate)
    {
        return PatientCareAssignment.Assign(
            id: PatientCareAssignmentId.New(),
            simulationHostId: simulationHostId,
            patientId: new PatientId(patientId),
            careFacilityId: careFacilityId,
            careDate: careDate,
            urgency: CareNeedUrgency.Urgent,
            assessmentRevision: 17,
            lifecycleRevision: 0,
            assignedAtUtc: AssignedAtUtc);
    }
}
