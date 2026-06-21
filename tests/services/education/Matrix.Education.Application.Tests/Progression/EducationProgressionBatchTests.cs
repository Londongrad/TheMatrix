using Matrix.Education.Application.Progression;
using Matrix.Education.Domain.Simulation;
using Xunit;

namespace Matrix.Education.Application.Tests.Progression
{
    public sealed class EducationProgressionBatchTests
    {
        private static readonly DateTimeOffset FromUtc =
            new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Create_WhenArgumentsAreValid_SetsProperties()
        {
            var hostId = new SimulationHostId(Guid.NewGuid());

            EducationProgressionBatch batch = EducationProgressionBatch.Create(
                simulationHostId: hostId,
                tickId: 42,
                fromSimTimeUtc: FromUtc,
                toSimTimeUtc: FromUtc.AddHours(6));

            Assert.Equal(hostId, batch.SimulationHostId);
            Assert.Equal(42, batch.TickId);
            Assert.Equal(FromUtc, batch.FromSimTimeUtc);
            Assert.Equal(FromUtc.AddHours(6), batch.ToSimTimeUtc);
        }

        [Fact]
        public void Create_WhenTickIsNegative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EducationProgressionBatch.Create(
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                tickId: -1,
                fromSimTimeUtc: FromUtc,
                toSimTimeUtc: FromUtc));
        }

        [Fact]
        public void Create_WhenTimeMovesBackwards_Throws()
        {
            Assert.Throws<ArgumentException>(() => EducationProgressionBatch.Create(
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                tickId: 1,
                fromSimTimeUtc: FromUtc,
                toSimTimeUtc: FromUtc.AddTicks(-1)));
        }

        [Fact]
        public void Create_WhenTimestampIsNotUtc_Throws()
        {
            Assert.Throws<ArgumentException>(() => EducationProgressionBatch.Create(
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                tickId: 1,
                fromSimTimeUtc: FromUtc.ToOffset(TimeSpan.FromHours(3)),
                toSimTimeUtc: FromUtc.AddHours(1)));
        }
    }
}
