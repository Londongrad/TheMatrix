using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class ResidentCapacityTests
    {
        [Fact]
        public void From_AcceptsBoundaries()
        {
            Assert.Equal(
                expected: ResidentCapacity.Min,
                actual: ResidentCapacity.From(ResidentCapacity.Min)
                   .Value);
            Assert.Equal(
                expected: ResidentCapacity.Max,
                actual: ResidentCapacity.From(ResidentCapacity.Max)
                   .Value);
        }

        [Fact]
        public void From_WhenBelowMinimum_ThrowsDomainException()
        {
            DomainException exception =
                Assert.Throws<DomainException>(() => ResidentCapacity.From(ResidentCapacity.Min - 1));

            Assert.Equal(
                expected: "SimulationCore.Topology.ResidentialBuilding.Capacity.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void From_WhenAboveMaximum_ThrowsDomainException()
        {
            DomainException exception =
                Assert.Throws<DomainException>(() => ResidentCapacity.From(ResidentCapacity.Max + 1));

            Assert.Equal(
                expected: "SimulationCore.Topology.ResidentialBuilding.Capacity.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ToString_ReturnsNumericValue()
        {
            var capacity = ResidentCapacity.From(320);

            Assert.Equal(
                expected: "320",
                actual: capacity.ToString());
        }
    }
}
