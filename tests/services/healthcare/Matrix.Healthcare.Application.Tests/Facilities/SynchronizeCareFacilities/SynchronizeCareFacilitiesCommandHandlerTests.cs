using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Facilities.SynchronizeCareFacilities
{
    public sealed class SynchronizeCareFacilitiesCommandHandlerTests
    {
        private static readonly Guid HostId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTimeOffset SynchronizedAtUtc =
            DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

        [Fact]
        public async Task Handle_MixedBatch_UsesBulkReadAndSingleSave()
        {
            Guid updatedId = Guid.NewGuid();
            Guid ignoredId = Guid.NewGuid();
            Guid addedId = Guid.NewGuid();
            CareFacility updated = CreateFacility(updatedId, sourceRevision: 4);
            CareFacility ignored = CreateFacility(ignoredId, sourceRevision: 8);
            var repository = new CareFacilityRepositoryStub([updated, ignored]);
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var handler = new SynchronizeCareFacilitiesCommandHandler(
                repository,
                new HealthcareSimulationDeletionRepositoryStub(),
                unitOfWork);

            SynchronizeCareFacilitiesResult result = await handler.Handle(
                CreateCommand(
                    sourceRevision: 5,
                    CreateItem(
                        updatedId,
                        name: "Regional Clinic",
                        kind: "PrimaryCare",
                        capacity: 80,
                        isActive: false),
                    CreateItem(ignoredId),
                    CreateItem(addedId, name: "North Hospital", capacity: 120)),
                CancellationToken.None);

            Assert.Equal(SynchronizeCareFacilitiesStatus.Applied, result.Status);
            Assert.Equal(1, result.AddedFacilities);
            Assert.Equal(1, result.UpdatedFacilities);
            Assert.Equal(1, result.IgnoredFacilities);
            Assert.Equal(3, result.ProcessedFacilities);
            Assert.Equal(1, repository.GetCallCount);
            Assert.Equal(3, repository.RequestedIds.Count);
            Assert.Equal(1, repository.AddRangeCallCount);
            CareFacility added = Assert.Single(repository.AddedFacilities);
            Assert.Equal(new CareFacilityId(addedId), added.CareFacilityId);
            Assert.Equal("Regional Clinic", updated.Name);
            Assert.Equal(80, updated.DailyPatientCapacity);
            Assert.False(updated.IsActive);
            Assert.Equal(8, ignored.LastSourceRevision);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_IgnoresBatchWithoutLoadingFacilities()
        {
            var repository = new CareFacilityRepositoryStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var handler = new SynchronizeCareFacilitiesCommandHandler(
                repository,
                new HealthcareSimulationDeletionRepositoryStub(SynchronizedAtUtc),
                unitOfWork);

            SynchronizeCareFacilitiesResult result = await handler.Handle(
                CreateCommand(sourceRevision: 5, CreateItem(Guid.NewGuid())),
                CancellationToken.None);

            Assert.Equal(SynchronizeCareFacilitiesStatus.SimulationDeleted, result.Status);
            Assert.Equal(1, result.IgnoredFacilities);
            Assert.Equal(0, repository.GetCallCount);
            Assert.Equal(0, repository.AddRangeCallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DuplicateFacilityId_RejectsBatchBeforeTransaction()
        {
            Guid facilityId = Guid.NewGuid();
            var repository = new CareFacilityRepositoryStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            var handler = new SynchronizeCareFacilitiesCommandHandler(
                repository,
                new HealthcareSimulationDeletionRepositoryStub(),
                unitOfWork);

            await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
                CreateCommand(
                    sourceRevision: 5,
                    CreateItem(facilityId),
                    CreateItem(facilityId)),
                CancellationToken.None));

            Assert.Equal(0, unitOfWork.TransactionCount);
            Assert.Equal(0, repository.GetCallCount);
        }

        private static CareFacility CreateFacility(Guid facilityId, long sourceRevision)
        {
            return CareFacility.Register(
                id: new CareFacilityId(facilityId),
                simulationHostId: new SimulationHostId(HostId),
                name: "Central Hospital",
                kind: new CareFacilityKindKey("Hospital"),
                locationAnchorId: null,
                dailyPatientCapacity: 240,
                isActive: true,
                sourceRevision: sourceRevision,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(-1));
        }

        private static SynchronizeCareFacilityItem CreateItem(
            Guid facilityId,
            string name = "Central Hospital",
            string kind = "Hospital",
            int capacity = 240,
            bool isActive = true)
        {
            return new SynchronizeCareFacilityItem(
                FacilityId: facilityId,
                Name: name,
                Kind: kind,
                LocationAnchorId: null,
                DailyPatientCapacity: capacity,
                IsActive: isActive);
        }

        private static SynchronizeCareFacilitiesCommand CreateCommand(
            long sourceRevision,
            params SynchronizeCareFacilityItem[] facilities)
        {
            return new SynchronizeCareFacilitiesCommand(
                SimulationHostId: HostId,
                SourceRevision: sourceRevision,
                SynchronizedAtUtc: SynchronizedAtUtc,
                Facilities: facilities);
        }

        private sealed class CareFacilityRepositoryStub(
            IReadOnlyList<CareFacility>? existingFacilities = null)
            : ICareFacilityRepository
        {
            private readonly IReadOnlyList<CareFacility> _existingFacilities =
                existingFacilities ?? Array.Empty<CareFacility>();

            internal int GetCallCount { get; private set; }
            internal int AddRangeCallCount { get; private set; }
            internal IReadOnlyCollection<CareFacilityId> RequestedIds { get; private set; } =
                Array.Empty<CareFacilityId>();
            internal IReadOnlyCollection<CareFacility> AddedFacilities { get; private set; } =
                Array.Empty<CareFacility>();

            public Task<IReadOnlyList<CareFacility>> GetByIdsAsync(
                IReadOnlyCollection<CareFacilityId> facilityIds,
                CancellationToken cancellationToken = default)
            {
                GetCallCount++;
                RequestedIds = facilityIds;
                return Task.FromResult(_existingFacilities);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<CareFacility> facilities,
                CancellationToken cancellationToken = default)
            {
                AddRangeCallCount++;
                AddedFacilities = facilities;
                return Task.CompletedTask;
            }
        }
    }
}
