using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Resources.Domain.Simulation;
using Xunit;

namespace Matrix.Resources.Domain.Tests.Simulation
{
    public sealed class SimulationHostIdTests
    {
        [Fact]
        public void Constructor_WithValidGuid_CreatesIdentifier()
        {
            var value = Guid.Parse("30000000-0000-0000-0000-000000000001");

            var id = new SimulationHostId(value);

            Assert.Equal(
                expected: value,
                actual: id.Value);
            Assert.Equal(
                expected: value.ToString(),
                actual: id.ToString());
        }

        [Fact]
        public void Constructor_WithEmptyGuid_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new SimulationHostId(Guid.Empty));

            Assert.Equal(
                expected: "Resources.SimulationHost.Id.Empty",
                actual: exception.Code);
        }
    }
}
