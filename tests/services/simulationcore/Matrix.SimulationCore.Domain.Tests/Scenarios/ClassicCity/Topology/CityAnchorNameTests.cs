using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class CityAnchorNameTests
    {
        [Fact]
        public void Constructor_TrimsAndStoresValue()
        {
            var name = new CityAnchorName("  Central Station  ");

            Assert.Equal(
                expected: "Central Station",
                actual: name.Value);
            Assert.Equal(
                expected: "Central Station",
                actual: name.ToString());
        }

        [Fact]
        public void Constructor_WhenValueIsNull_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityAnchorName(null));

            Assert.Equal(
                expected: "SimulationCore.Topology.CityAnchor.Name.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityAnchorName("   "));

            Assert.Equal(
                expected: "SimulationCore.Topology.CityAnchor.Name.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
        {
            string tooLong = new(
                c: 'a',
                count: CityAnchorName.MaxLength + 1);

            DomainException exception = Assert.Throws<DomainException>(() => new CityAnchorName(tooLong));

            Assert.Equal(
                expected: "SimulationCore.Topology.CityAnchor.Name.TooLong",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }
    }
}
