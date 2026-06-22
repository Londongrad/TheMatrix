using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Education.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.Education.Application.Tests.Students.SynchronizeStudentProfiles
{
    public sealed class SynchronizeStudentProfilesValidationTests
    {
        [Fact]
        public async Task Handle_EmptyBatch_RejectsBeforeTransaction()
        {
            var context = new StudentProfileSynchronizationTestContext();

            await AssertRejectedBeforeTransaction<ArgumentException>(
                context,
                StudentProfileSynchronizationTestData.CreateCommand());
        }

        [Fact]
        public async Task Handle_OversizedBatch_RejectsBeforeTransaction()
        {
            var context = new StudentProfileSynchronizationTestContext();
            SynchronizeStudentProfileItem[] items = Enumerable.Range(
                    start: 0,
                    count: SynchronizeStudentProfilesCommandHandler.MaxBatchSize + 1)
               .Select(_ => StudentProfileSynchronizationTestData.CreateItem(
                    residentId: Guid.NewGuid(),
                    sourceRevision: 1))
               .ToArray();

            await AssertRejectedBeforeTransaction<ArgumentOutOfRangeException>(
                context,
                StudentProfileSynchronizationTestData.CreateCommand(items));
        }

        [Fact]
        public async Task Handle_DuplicateResident_RejectsBeforeTransaction()
        {
            var context = new StudentProfileSynchronizationTestContext();
            Guid residentId = Guid.NewGuid();

            await AssertRejectedBeforeTransaction<ArgumentException>(
                context,
                StudentProfileSynchronizationTestData.CreateCommand(
                    StudentProfileSynchronizationTestData.CreateItem(residentId, sourceRevision: 1),
                    StudentProfileSynchronizationTestData.CreateItem(residentId, sourceRevision: 2)));
        }

        [Fact]
        public async Task Handle_NegativeRevision_RejectsBeforeTransaction()
        {
            var context = new StudentProfileSynchronizationTestContext();

            await AssertRejectedBeforeTransaction<ArgumentOutOfRangeException>(
                context,
                StudentProfileSynchronizationTestData.CreateCommand(
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: Guid.NewGuid(),
                        sourceRevision: -1)));
        }

        [Fact]
        public async Task Handle_NonUtcTimestamp_RejectsBeforeTransaction()
        {
            var context = new StudentProfileSynchronizationTestContext();
            var command = new SynchronizeStudentProfilesCommand(
                SimulationHostId: StudentProfileSynchronizationTestData.HostId,
                SynchronizedAtUtc: StudentProfileSynchronizationTestData.SynchronizedAtUtc
                   .ToOffset(TimeSpan.FromHours(3)),
                Profiles: new[]
                {
                    StudentProfileSynchronizationTestData.CreateItem(
                        residentId: Guid.NewGuid(),
                        sourceRevision: 1)
                });

            await AssertRejectedBeforeTransaction<ArgumentException>(context, command);
        }

        private static async Task AssertRejectedBeforeTransaction<TException>(
            StudentProfileSynchronizationTestContext context,
            SynchronizeStudentProfilesCommand command)
            where TException : Exception
        {
            SynchronizeStudentProfilesCommandHandler handler = context.CreateHandler();

            await Assert.ThrowsAsync<TException>(() => handler.Handle(
                command,
                CancellationToken.None));

            Assert.Equal(0, context.UnitOfWork.TransactionCount);
            Assert.Equal(0, context.Repository.GetCallCount);
        }
    }
}
