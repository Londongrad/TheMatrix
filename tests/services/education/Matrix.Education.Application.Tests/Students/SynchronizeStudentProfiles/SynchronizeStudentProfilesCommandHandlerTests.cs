using System.Data;
using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Students.SynchronizeStudentProfiles
{
    public sealed class SynchronizeStudentProfilesCommandHandlerTests
    {
        [Fact]
        public async Task Handle_MixedBatch_UsesOneReadOneAddRangeAndOneSave()
        {
            Guid updatedId = Guid.NewGuid();
            Guid ignoredId = Guid.NewGuid();
            Guid addedId = Guid.NewGuid();
            StudentProfile updatedProfile = StudentProfileSynchronizationTestData.CreateProfile(
                residentId: updatedId,
                sourceRevision: 4);
            StudentProfile ignoredProfile = StudentProfileSynchronizationTestData.CreateProfile(
                residentId: ignoredId,
                sourceRevision: 8);
            var context = new StudentProfileSynchronizationTestContext(
                new[] { updatedProfile, ignoredProfile });
            SynchronizeStudentProfilesCommandHandler handler = context.CreateHandler();

            SynchronizeStudentProfilesResult result = await handler.Handle(
                StudentProfileSynchronizationTestData.CreateCommand(
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: updatedId,
                        sourceRevision: 5,
                        birthDate: new DateOnly(2031, 2, 3),
                        isAlive: false,
                        isActive: false),
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: ignoredId,
                        sourceRevision: 7,
                        birthDate: new DateOnly(2000, 1, 1)),
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: addedId,
                        sourceRevision: 1)),
                CancellationToken.None);

            Assert.Equal(SynchronizeStudentProfilesStatus.Applied, result.Status);
            Assert.Equal(1, result.AddedProfiles);
            Assert.Equal(1, result.UpdatedProfiles);
            Assert.Equal(1, result.IgnoredProfiles);
            Assert.Equal(3, result.ProcessedProfiles);
            Assert.Equal(1, context.Repository.GetCallCount);
            Assert.Equal(3, context.Repository.RequestedIds.Count);
            Assert.Equal(1, context.Repository.AddRangeCallCount);
            StudentProfile addedProfile = Assert.Single(context.Repository.AddedProfiles);
            Assert.Equal(new ResidentId(addedId), addedProfile.ResidentId);
            Assert.Equal(1, context.UnitOfWork.SaveCount);
            Assert.Equal(1, context.UnitOfWork.TransactionCount);
            Assert.Equal(IsolationLevel.Serializable, context.UnitOfWork.LastIsolationLevel);
            Assert.Equal(new DateOnly(2031, 2, 3), updatedProfile.BirthDate);
            Assert.False(updatedProfile.IsAlive);
            Assert.False(updatedProfile.IsActive);
            Assert.Equal(8, ignoredProfile.LastSourceRevision);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_IgnoresBatchWithoutLoadingProfiles()
        {
            var context = new StudentProfileSynchronizationTestContext(
                deletedAtUtc: StudentProfileSynchronizationTestData.SynchronizedAtUtc.AddMinutes(-1));
            SynchronizeStudentProfilesCommandHandler handler = context.CreateHandler();

            SynchronizeStudentProfilesResult result = await handler.Handle(
                StudentProfileSynchronizationTestData.CreateCommand(
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: Guid.NewGuid(),
                        sourceRevision: 10)),
                CancellationToken.None);

            Assert.Equal(SynchronizeStudentProfilesStatus.SimulationDeleted, result.Status);
            Assert.Equal(1, result.IgnoredProfiles);
            Assert.Equal(0, context.Repository.GetCallCount);
            Assert.Equal(0, context.Repository.AddRangeCallCount);
            Assert.Equal(0, context.UnitOfWork.SaveCount);
            Assert.Equal(1, context.DeletionRepository.GetCallCount);
        }

        [Fact]
        public async Task Handle_AllStaleBatch_DoesNotIssueWrite()
        {
            Guid residentId = Guid.NewGuid();
            StudentProfile existing = StudentProfileSynchronizationTestData.CreateProfile(
                residentId: residentId,
                sourceRevision: 8);
            var context = new StudentProfileSynchronizationTestContext(new[] { existing });
            SynchronizeStudentProfilesCommandHandler handler = context.CreateHandler();

            SynchronizeStudentProfilesResult result = await handler.Handle(
                StudentProfileSynchronizationTestData.CreateCommand(
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: residentId,
                        sourceRevision: 8)),
                CancellationToken.None);

            Assert.Equal(1, result.IgnoredProfiles);
            Assert.Equal(1, context.Repository.GetCallCount);
            Assert.Equal(0, context.Repository.AddRangeCallCount);
            Assert.Equal(0, context.UnitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_ExistingProfileFromAnotherHost_RejectsBatch()
        {
            Guid residentId = Guid.NewGuid();
            StudentProfile existing = StudentProfileSynchronizationTestData.CreateProfile(
                residentId: residentId,
                sourceRevision: 1,
                simulationHostId: Guid.NewGuid());
            var context = new StudentProfileSynchronizationTestContext(new[] { existing });
            SynchronizeStudentProfilesCommandHandler handler = context.CreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
                StudentProfileSynchronizationTestData.CreateCommand(
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: residentId,
                        sourceRevision: 2)),
                CancellationToken.None));

            Assert.Equal(0, context.Repository.AddRangeCallCount);
            Assert.Equal(0, context.UnitOfWork.SaveCount);
        }
    }
}
