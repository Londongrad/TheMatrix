using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation
{
    public sealed class TickIdTests
    {
        [Fact]
        public void Start_ReturnsZero()
        {
            Assert.Equal(
                expected: 0,
                actual: TickId.Start()
                   .Value);
        }

        [Fact]
        public void Next_IncrementsValueMonotonically()
        {
            TickId next = new TickId(41).Next();

            Assert.Equal(
                expected: 42,
                actual: next.Value);
        }
    }
}
