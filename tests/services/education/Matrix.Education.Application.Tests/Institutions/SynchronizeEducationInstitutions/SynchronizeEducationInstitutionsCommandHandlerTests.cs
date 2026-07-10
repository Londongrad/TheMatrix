using System.Data;
using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Xunit;

namespace Matrix.Education.Application.Tests.Institutions.SynchronizeEducationInstitutions
{
    public sealed class SynchronizeEducationInstitutionsCommandHandlerTests
    {
        private static readonly SimulationHostId SimulationHostId =
            new(StudentProfileSynchronizationTestData.HostId);
        private static readonly DateTimeOffset SynchronizedAtUtc =
            new(2048, 5, 1, 10, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Handle_MixedBatch_UsesOneLookupAndOneAddRange()
        {
            EducationInstitution updated = CreateInstitution("Old academy");
            updated.TrySynchronizeProvisioning(
                4,
                updated.Name,
                updated.Kind,
                updated.Capacity,
                true,
                SynchronizedAtUtc.AddMinutes(-2));
            EducationInstitution stale = CreateInstitution("New university");
            stale.TrySynchronizeProvisioning(
                8,
                stale.Name,
                stale.Kind,
                stale.Capacity,
                true,
                SynchronizedAtUtc.AddMinutes(-1));
            Guid addedId = Guid.NewGuid();
            var repository = new EducationInstitutionRepositoryStub([updated, stale]);
            var unitOfWork = new EducationUnitOfWorkStub();
            var handler = new SynchronizeEducationInstitutionsCommandHandler(
                repository,
                new EducationSimulationDeletionRepositoryStub(),
                unitOfWork);

            SynchronizeEducationInstitutionsResult result = await handler.Handle(
                CreateCommand(
                    new SynchronizeEducationInstitutionItem(
                        updated.EducationInstitutionId.Value,
                        "Updated academy",
                        "academy",
                        200,
                        true),
                    new SynchronizeEducationInstitutionItem(
                        stale.EducationInstitutionId.Value,
                        "Stale overwrite",
                        "university",
                        10,
                        false),
                    new SynchronizeEducationInstitutionItem(
                        addedId,
                        "New school",
                        "school",
                        80,
                        true)),
                CancellationToken.None);

            Assert.Equal(SynchronizeEducationInstitutionsStatus.Applied, result.Status);
            Assert.Equal(1, result.AddedInstitutions);
            Assert.Equal(1, result.UpdatedInstitutions);
            Assert.Equal(1, result.IgnoredInstitutions);
            Assert.Equal("Updated academy", updated.Name);
            Assert.Equal("New university", stale.Name);
            Assert.Equal(1, repository.GetByIdsCallCount);
            Assert.Equal(1, repository.AddRangeCallCount);
            Assert.Equal(addedId, Assert.Single(repository.Added).EducationInstitutionId.Value);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_IgnoresBatchWithoutLoadingInstitutions()
        {
            var repository = new EducationInstitutionRepositoryStub(
                Array.Empty<EducationInstitution>());
            var unitOfWork = new EducationUnitOfWorkStub();
            var handler = new SynchronizeEducationInstitutionsCommandHandler(
                repository,
                new EducationSimulationDeletionRepositoryStub(SynchronizedAtUtc),
                unitOfWork);

            SynchronizeEducationInstitutionsResult result = await handler.Handle(
                CreateCommand(new SynchronizeEducationInstitutionItem(
                    Guid.NewGuid(),
                    "School",
                    "school",
                    80,
                    true)),
                CancellationToken.None);

            Assert.Equal(SynchronizeEducationInstitutionsStatus.SimulationDeleted, result.Status);
            Assert.Equal(1, result.IgnoredInstitutions);
            Assert.Equal(0, repository.GetByIdsCallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DuplicateIdentifiers_RejectsBatchBeforeTransaction()
        {
            Guid institutionId = Guid.NewGuid();
            var unitOfWork = new EducationUnitOfWorkStub();
            var handler = new SynchronizeEducationInstitutionsCommandHandler(
                new EducationInstitutionRepositoryStub(Array.Empty<EducationInstitution>()),
                new EducationSimulationDeletionRepositoryStub(),
                unitOfWork);
            SynchronizeEducationInstitutionItem item = new(
                institutionId,
                "School",
                "school",
                80,
                true);

            await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
                CreateCommand(item, item),
                CancellationToken.None));

            Assert.Equal(0, unitOfWork.TransactionCount);
        }

        private static SynchronizeEducationInstitutionsCommand CreateCommand(
            params SynchronizeEducationInstitutionItem[] institutions)
        {
            return new SynchronizeEducationInstitutionsCommand(
                SimulationHostId: SimulationHostId.Value,
                SourceRevision: 5,
                SynchronizedAtUtc: SynchronizedAtUtc,
                Institutions: institutions);
        }

        private static EducationInstitution CreateInstitution(string name)
        {
            return EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: SimulationHostId,
                name: name,
                kind: new EducationInstitutionKindKey("school"),
                capacity: 100);
        }
    }
}
