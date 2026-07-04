using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Care.AllocatePatientCare;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Care.AllocatePatientCare;

public sealed class PatientCareAllocatorTests
{
    private static readonly SimulationHostId SimulationHostId = new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly DateOnly CareDate = new(2048, 5, 6);
    private static readonly DateTimeOffset AssignedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task AllocateAsync_AvailableCapacity_AddsAssignmentsInBulk()
    {
        CareFacility facility = CreateFacility(capacity: 3);
        var repository = new AllocationRepositoryStub
        {
            Facilities = [facility],
            AssignmentCounts =
            [
                new CareFacilityAssignmentCount(
                    facility.CareFacilityId,
                    AssignedPatients: 1)
            ],
            CareNeeds =
            [
                CreateCareNeed("20000000-0000-0000-0000-000000000001", CareNeedUrgency.Emergency),
                CreateCareNeed("20000000-0000-0000-0000-000000000002", CareNeedUrgency.Urgent)
            ]
        };
        var allocator = new PatientCareAllocator(
            repository,
            new PatientCareAllocationPolicy());

        int assignmentCount = await allocator.AllocateAsync(
            SimulationHostId,
            CareDate,
            AssignedAtUtc);

        Assert.Equal(2, assignmentCount);
        Assert.Equal(2, repository.RequestedMaximumCareNeeds);
        Assert.Equal(2, repository.AddedAssignments.Count);
        Assert.All(repository.AddedAssignments, assignment =>
        {
            Assert.Equal(SimulationHostId, assignment.SimulationHostId);
            Assert.Equal(facility.CareFacilityId, assignment.CareFacilityId);
            Assert.Equal(CareDate, assignment.CareDate);
            Assert.Equal(AssignedAtUtc, assignment.AssignedAtUtc);
        });
        Assert.Contains(
            repository.AddedAssignments,
            assignment => assignment.Urgency == CareNeedUrgency.Emergency);
    }

    [Fact]
    public async Task AllocateAsync_FacilitiesAreFull_DoesNotQueryCareNeeds()
    {
        CareFacility facility = CreateFacility(capacity: 2);
        var repository = new AllocationRepositoryStub
        {
            Facilities = [facility],
            AssignmentCounts =
            [
                new CareFacilityAssignmentCount(
                    facility.CareFacilityId,
                    AssignedPatients: 2)
            ]
        };
        var allocator = new PatientCareAllocator(
            repository,
            new PatientCareAllocationPolicy());

        int assignmentCount = await allocator.AllocateAsync(
            SimulationHostId,
            CareDate,
            AssignedAtUtc);

        Assert.Equal(0, assignmentCount);
        Assert.Equal(0, repository.GetCareNeedsCallCount);
        Assert.Empty(repository.AddedAssignments);
    }

    [Fact]
    public async Task AllocateAsync_NoFacilities_DoesNotQueryUsageOrCareNeeds()
    {
        var repository = new AllocationRepositoryStub();
        var allocator = new PatientCareAllocator(
            repository,
            new PatientCareAllocationPolicy());

        int assignmentCount = await allocator.AllocateAsync(
            SimulationHostId,
            CareDate,
            AssignedAtUtc);

        Assert.Equal(0, assignmentCount);
        Assert.Equal(0, repository.GetAssignmentCountsCallCount);
        Assert.Equal(0, repository.GetCareNeedsCallCount);
    }

    [Fact]
    public async Task AllocateAsync_LargeCapacity_BoundsCandidateWorkingSet()
    {
        var repository = new AllocationRepositoryStub
        {
            Facilities = [CreateFacility(capacity: 20_000)]
        };
        var allocator = new PatientCareAllocator(
            repository,
            new PatientCareAllocationPolicy());

        int assignmentCount = await allocator.AllocateAsync(
            SimulationHostId,
            CareDate,
            AssignedAtUtc);

        Assert.Equal(0, assignmentCount);
        Assert.Equal(
            PatientCareAllocator.MaxAssignmentsPerRun,
            repository.RequestedMaximumCareNeeds);
    }

    [Fact]
    public async Task AllocateAsync_NonUtcTimestamp_DoesNotQueryPersistence()
    {
        var repository = new AllocationRepositoryStub();
        var allocator = new PatientCareAllocator(
            repository,
            new PatientCareAllocationPolicy());

        await Assert.ThrowsAsync<ArgumentException>(() => allocator.AllocateAsync(
            SimulationHostId,
            CareDate,
            AssignedAtUtc.ToOffset(TimeSpan.FromHours(3))));

        Assert.Equal(0, repository.GetFacilitiesCallCount);
    }

    private static PatientCareNeed CreateCareNeed(
        string patientId,
        CareNeedUrgency urgency)
    {
        return PatientCareNeed.Register(
            patientId: new PatientId(Guid.Parse(patientId)),
            simulationHostId: SimulationHostId,
            urgency: urgency,
            requestedOn: CareDate,
            assessmentRevision: 17,
            lifecycleRevision: 2,
            assessedAtUtc: AssignedAtUtc);
    }

    private static CareFacility CreateFacility(int capacity)
    {
        return CareFacility.Register(
            id: new CareFacilityId(
                Guid.Parse("30000000-0000-0000-0000-000000000001")),
            simulationHostId: SimulationHostId,
            name: "Central Hospital",
            kind: new CareFacilityKindKey("Hospital"),
            locationAnchorId: null,
            dailyPatientCapacity: capacity,
            isActive: true,
            sourceRevision: 7,
            synchronizedAtUtc: AssignedAtUtc);
    }

    private sealed class AllocationRepositoryStub : IPatientCareAllocationRepository
    {
        internal IReadOnlyList<CareFacility> Facilities { get; init; } = [];
        internal IReadOnlyList<CareFacilityAssignmentCount> AssignmentCounts { get; init; } = [];
        internal IReadOnlyList<PatientCareNeed> CareNeeds { get; init; } = [];
        internal List<PatientCareAssignment> AddedAssignments { get; } = [];
        internal int GetFacilitiesCallCount { get; private set; }
        internal int GetAssignmentCountsCallCount { get; private set; }
        internal int GetCareNeedsCallCount { get; private set; }
        internal int? RequestedMaximumCareNeeds { get; private set; }

        public Task<IReadOnlyList<CareFacility>> GetActiveFacilitiesAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            GetFacilitiesCallCount++;
            return Task.FromResult(Facilities);
        }

        public Task<IReadOnlyList<CareFacilityAssignmentCount>> GetAssignmentCountsAsync(
            SimulationHostId simulationHostId,
            DateOnly careDate,
            IReadOnlyCollection<CareFacilityId> careFacilityIds,
            CancellationToken cancellationToken = default)
        {
            GetAssignmentCountsCallCount++;
            return Task.FromResult(AssignmentCounts);
        }

        public Task<IReadOnlyList<PatientCareNeed>> GetUnassignedCareNeedsAsync(
            SimulationHostId simulationHostId,
            DateOnly careDate,
            int maximumCount,
            CancellationToken cancellationToken = default)
        {
            GetCareNeedsCallCount++;
            RequestedMaximumCareNeeds = maximumCount;
            return Task.FromResult(CareNeeds);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<PatientCareAssignment> assignments,
            CancellationToken cancellationToken = default)
        {
            AddedAssignments.AddRange(assignments);
            return Task.CompletedTask;
        }
    }
}
