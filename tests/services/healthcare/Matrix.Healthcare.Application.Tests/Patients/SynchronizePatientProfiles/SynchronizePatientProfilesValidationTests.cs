using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Patients;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Patients.SynchronizePatientProfiles
{
    public sealed class SynchronizePatientProfilesValidationTests
    {
        [Fact]
        public async Task Handle_EmptyBatch_RejectsBeforeTransaction()
        {
            await AssertRejectedBeforeTransaction<ArgumentException>(
                PatientProfileSynchronizationTestData.CreateCommand());
        }

        [Fact]
        public async Task Handle_OversizedBatch_RejectsBeforeTransaction()
        {
            SynchronizePatientProfileItem[] items = Enumerable.Range(
                    start: 0,
                    count: SynchronizePatientProfilesCommandHandler.MaxBatchSize + 1)
               .Select(_ => PatientProfileSynchronizationTestData.CreateItem(
                    patientId: Guid.NewGuid(),
                    sourceRevision: 1))
               .ToArray();

            await AssertRejectedBeforeTransaction<ArgumentOutOfRangeException>(
                PatientProfileSynchronizationTestData.CreateCommand(items));
        }

        [Fact]
        public async Task Handle_DuplicatePatient_RejectsBeforeTransaction()
        {
            Guid patientId = Guid.NewGuid();

            await AssertRejectedBeforeTransaction<ArgumentException>(
                PatientProfileSynchronizationTestData.CreateCommand(
                    PatientProfileSynchronizationTestData.CreateItem(patientId, sourceRevision: 1),
                    PatientProfileSynchronizationTestData.CreateItem(patientId, sourceRevision: 2)));
        }

        [Fact]
        public async Task Handle_NegativeRevision_RejectsBeforeTransaction()
        {
            await AssertRejectedBeforeTransaction<ArgumentOutOfRangeException>(
                PatientProfileSynchronizationTestData.CreateCommand(
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: Guid.NewGuid(),
                        sourceRevision: -1)));
        }

        [Fact]
        public async Task Handle_UnsupportedSex_RejectsBeforeTransaction()
        {
            await AssertRejectedBeforeTransaction<ArgumentOutOfRangeException>(
                PatientProfileSynchronizationTestData.CreateCommand(
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: Guid.NewGuid(),
                        sourceRevision: 1,
                        sex: (PatientSex)99)));
        }

        [Fact]
        public async Task Handle_NonUtcTimestamp_RejectsBeforeTransaction()
        {
            var command = new SynchronizePatientProfilesCommand(
                SimulationHostId: PatientProfileSynchronizationTestData.HostId,
                SynchronizedAtUtc: PatientProfileSynchronizationTestData.SynchronizedAtUtc
                   .ToOffset(TimeSpan.FromHours(3)),
                Profiles:
                [
                    PatientProfileSynchronizationTestData.CreateItem(
                        patientId: Guid.NewGuid(),
                        sourceRevision: 1)
                ]);

            await AssertRejectedBeforeTransaction<ArgumentException>(command);
        }

        private static async Task AssertRejectedBeforeTransaction<TException>(
            SynchronizePatientProfilesCommand command)
            where TException : Exception
        {
            var context = new PatientProfileSynchronizationTestContext();

            await Assert.ThrowsAsync<TException>(() => context.CreateHandler().Handle(
                command,
                CancellationToken.None));

            Assert.Equal(0, context.UnitOfWork.TransactionCount);
            Assert.Equal(0, context.Repository.GetCallCount);
        }
    }
}
