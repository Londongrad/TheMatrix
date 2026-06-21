using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;
using Xunit;

namespace Matrix.Education.Domain.Tests.Progression
{
    public sealed class EducationProgressionCheckpointTests
    {
        private static readonly DateTimeOffset InitialCompletedAtUtc =
            new(2048, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(12, ProgressionTickDisposition.Duplicate)]
        [InlineData(11, ProgressionTickDisposition.OutOfOrder)]
        [InlineData(13, ProgressionTickDisposition.Accepted)]
        [InlineData(18, ProgressionTickDisposition.Accepted)]
        public void Classify_ReturnsDispositionWithoutMutatingCheckpoint(
            long tickId,
            ProgressionTickDisposition expected)
        {
            EducationProgressionCheckpoint checkpoint = CreateCheckpoint();

            ProgressionTickDisposition actual = checkpoint.Classify(tickId);

            Assert.Equal(expected, actual);
            Assert.Equal(12, checkpoint.LastCompletedTickId);
        }

        [Fact]
        public void MarkCompleted_AdvancesCheckpoint()
        {
            EducationProgressionCheckpoint checkpoint = CreateCheckpoint();
            DateTimeOffset completedAtUtc = InitialCompletedAtUtc.AddHours(6);
            DateTimeOffset updatedAtUtc = InitialCompletedAtUtc.AddMinutes(1);

            checkpoint.MarkCompleted(
                tickId: 13,
                completedAtUtc: completedAtUtc,
                updatedAtUtc: updatedAtUtc);

            Assert.Equal(13, checkpoint.LastCompletedTickId);
            Assert.Equal(completedAtUtc, checkpoint.LastCompletedAtUtc);
            Assert.Equal(updatedAtUtc, checkpoint.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(12)]
        [InlineData(11)]
        public void MarkCompleted_RejectsAlreadyObservedTick(long tickId)
        {
            EducationProgressionCheckpoint checkpoint = CreateCheckpoint();

            Assert.Throws<InvalidOperationException>(() => checkpoint.MarkCompleted(
                tickId: tickId,
                completedAtUtc: InitialCompletedAtUtc.AddHours(1),
                updatedAtUtc: InitialCompletedAtUtc.AddMinutes(1)));
        }

        [Fact]
        public void MarkCompleted_RejectsSimulationTimeMovingBackwards()
        {
            EducationProgressionCheckpoint checkpoint = CreateCheckpoint();

            Assert.Throws<InvalidOperationException>(() => checkpoint.MarkCompleted(
                tickId: 13,
                completedAtUtc: InitialCompletedAtUtc.AddTicks(-1),
                updatedAtUtc: InitialCompletedAtUtc.AddMinutes(1)));
        }

        [Fact]
        public void CreateCompleted_RejectsNonUtcTimestamp()
        {
            Assert.Throws<ArgumentException>(() => EducationProgressionCheckpoint.CreateCompleted(
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                tickId: 1,
                completedAtUtc: InitialCompletedAtUtc.ToOffset(TimeSpan.FromHours(3)),
                updatedAtUtc: InitialCompletedAtUtc));
        }

        private static EducationProgressionCheckpoint CreateCheckpoint()
        {
            return EducationProgressionCheckpoint.CreateCompleted(
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                tickId: 12,
                completedAtUtc: InitialCompletedAtUtc,
                updatedAtUtc: InitialCompletedAtUtc);
        }
    }
}
