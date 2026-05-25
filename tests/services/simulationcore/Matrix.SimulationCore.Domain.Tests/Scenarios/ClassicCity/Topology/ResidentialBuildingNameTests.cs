using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class ResidentialBuildingNameTests
    {
        [Fact]
        public void Constructor_TrimsAndStoresValue()
        {
            var name = new ResidentialBuildingName("  Tower A  ");

            Assert.Equal(
                expected: "Tower A",
                actual: name.Value);
            Assert.Equal(
                expected: "Tower A",
                actual: name.ToString());
        }

        [Fact]
        public void Constructor_WhenValueIsNull_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new ResidentialBuildingName(null));

            Assert.Equal(
                expected: "SimulationCore.Topology.ResidentialBuilding.Name.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new ResidentialBuildingName("   "));

            Assert.Equal(
                expected: "SimulationCore.Topology.ResidentialBuilding.Name.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
        {
            string tooLong = new(
                c: 'b',
                count: ResidentialBuildingName.MaxLength + 1);

            DomainException exception = Assert.Throws<DomainException>(() => new ResidentialBuildingName(tooLong));

            Assert.Equal(
                expected: "SimulationCore.Topology.ResidentialBuilding.Name.TooLong",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }
    }
}
