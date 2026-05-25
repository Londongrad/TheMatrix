using Matrix.SimulationSystems.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Simulation
{
    public sealed class SimulationHostIdTests
    {
        [Fact]
        public void Constructor_WhenGuidIsEmpty_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new SimulationHostId(Guid.Empty));
        }

        [Fact]
        public void New_CreatesNonEmptyIdentifier()
        {
            var id = SimulationHostId.New();

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: id.Value);
            Assert.Equal(
                expected: id.Value.ToString(),
                actual: id.ToString());
        }
    }
}
