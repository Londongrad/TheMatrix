using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class DistrictNameTests
    {
        [Fact]
        public void Constructor_TrimsAndStoresValue()
        {
            var name = new DistrictName("  Old Town  ");

            Assert.Equal(
                expected: "Old Town",
                actual: name.Value);
            Assert.Equal(
                expected: "Old Town",
                actual: name.ToString());
        }

        [Fact]
        public void Constructor_WhenValueIsNull_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new DistrictName(null));

            Assert.Equal(
                expected: "SimulationCore.Topology.District.Name.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new DistrictName("  "));

            Assert.Equal(
                expected: "SimulationCore.Topology.District.Name.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
        {
            string tooLong = new(
                c: 'd',
                count: DistrictName.MaxLength + 1);

            DomainException exception = Assert.Throws<DomainException>(() => new DistrictName(tooLong));

            Assert.Equal(
                expected: "SimulationCore.Topology.District.Name.TooLong",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }
    }
}
