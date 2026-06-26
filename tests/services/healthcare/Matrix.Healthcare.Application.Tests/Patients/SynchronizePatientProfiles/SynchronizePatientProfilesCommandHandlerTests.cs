using System.Data;
using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Patients;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Patients.SynchronizePatientProfiles
{
    public sealed class SynchronizePatientProfilesCommandHandlerTests
    {
        [Fact]
        public async Task Handle_MixedBatch_UsesOneReadOneAddRangeAndOneSave()
        {
            Guid updatedId = Guid.NewGuid();
            Guid ignoredId = Guid.NewGuid();
            Guid addedId = Guid.NewGuid();
            PatientProfile updatedProfile = PatientProfileSynchronizationTestData.CreateProfile(
                patientId: updatedId,
                sourceRevision: 4);
            PatientProfile ignoredProfile = PatientProfileSynchronizationTestData.CreateProfile(
                patientId: ignoredId,
                sourceRevision: 8);
            var context = new PatientProfileSynchronizationTestContext(
                new[] { updatedProfile, ignoredProfile });

            SynchronizePatientProfilesResult result = await context.CreateHandler().Handle(
                PatientProfileSynchronizationTestData.CreateCommand(
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: updatedId,
                        sourceRevision: 5,
                        birthDate: new DateOnly(2031, 2, 3),
                        sex: PatientSex.Male,
                        isAlive: false,
                        isActive: false),
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: ignoredId,
                        sourceRevision: 7),
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: addedId,
                        sourceRevision: 1)),
                CancellationToken.None);

            Assert.Equal(1, result.AddedProfiles);
            Assert.Equal(SynchronizePatientProfilesStatus.Applied, result.Status);
            Assert.Equal(1, result.UpdatedProfiles);
            Assert.Equal(1, result.IgnoredProfiles);
            Assert.Equal(3, result.ProcessedProfiles);
            Assert.Equal(1, context.Repository.GetCallCount);
            Assert.Equal(3, context.Repository.RequestedIds.Count);
            Assert.Equal(1, context.Repository.AddRangeCallCount);
            PatientProfile addedProfile = Assert.Single(context.Repository.AddedProfiles);
            Assert.Equal(new PatientId(addedId), addedProfile.PatientId);
            Assert.Equal(1, context.UnitOfWork.SaveCount);
            Assert.Equal(1, context.UnitOfWork.TransactionCount);
            Assert.Equal(IsolationLevel.Serializable, context.UnitOfWork.LastIsolationLevel);
            Assert.Equal(PatientSex.Male, updatedProfile.Sex);
            Assert.False(updatedProfile.IsEligibleForCare);
            Assert.Equal(8, ignoredProfile.LastSourceRevision);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_IgnoresBatchWithoutLoadingProfiles()
        {
            var context = new PatientProfileSynchronizationTestContext(
                deletedAtUtc: PatientProfileSynchronizationTestData.SynchronizedAtUtc.AddMinutes(-1));

            SynchronizePatientProfilesResult result = await context.CreateHandler().Handle(
                PatientProfileSynchronizationTestData.CreateCommand(
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: Guid.NewGuid(),
                        sourceRevision: 10)),
                CancellationToken.None);

            Assert.Equal(SynchronizePatientProfilesStatus.SimulationDeleted, result.Status);
            Assert.Equal(1, result.IgnoredProfiles);
            Assert.Equal(0, context.Repository.GetCallCount);
            Assert.Equal(0, context.Repository.AddRangeCallCount);
            Assert.Equal(0, context.UnitOfWork.SaveCount);
            Assert.Equal(1, context.DeletionRepository.GetCallCount);
        }

        [Fact]
        public async Task Handle_AllStaleBatch_DoesNotIssueWrite()
        {
            Guid patientId = Guid.NewGuid();
            PatientProfile existing = PatientProfileSynchronizationTestData.CreateProfile(
                patientId: patientId,
                sourceRevision: 8);
            var context = new PatientProfileSynchronizationTestContext(new[] { existing });

            SynchronizePatientProfilesResult result = await context.CreateHandler().Handle(
                PatientProfileSynchronizationTestData.CreateCommand(
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: patientId,
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
            Guid patientId = Guid.NewGuid();
            PatientProfile existing = PatientProfileSynchronizationTestData.CreateProfile(
                patientId: patientId,
                sourceRevision: 1,
                simulationHostId: Guid.NewGuid());
            var context = new PatientProfileSynchronizationTestContext(new[] { existing });

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.CreateHandler().Handle(
                PatientProfileSynchronizationTestData.CreateCommand(
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: patientId,
                        sourceRevision: 2)),
                CancellationToken.None));

            Assert.Equal(0, context.Repository.AddRangeCallCount);
            Assert.Equal(0, context.UnitOfWork.SaveCount);
        }
    }
}
