using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class TopologyIdentifierTests
    {
        [Fact]
        public void CityAnchorId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var value = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var identifier = new CityAnchorId(value);

            Assert.Equal(
                expected: value,
                actual: identifier.Value);
        }

        [Fact]
        public void CityAnchorId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityAnchorId(Guid.Empty));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void DistrictId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var value = Guid.Parse("10000000-0000-0000-0000-000000000002");
            var identifier = new DistrictId(value);

            Assert.Equal(
                expected: value,
                actual: identifier.Value);
        }

        [Fact]
        public void DistrictId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new DistrictId(Guid.Empty));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ResidentialBuildingId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var value = Guid.Parse("10000000-0000-0000-0000-000000000003");
            var identifier = new ResidentialBuildingId(value);

            Assert.Equal(
                expected: value,
                actual: identifier.Value);
        }

        [Fact]
        public void ResidentialBuildingId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new ResidentialBuildingId(Guid.Empty));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void RoadNodeId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var value = Guid.Parse("10000000-0000-0000-0000-000000000004");
            var identifier = new RoadNodeId(value);

            Assert.Equal(
                expected: value,
                actual: identifier.Value);
        }

        [Fact]
        public void RoadNodeId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new RoadNodeId(Guid.Empty));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void RoadSegmentId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var value = Guid.Parse("10000000-0000-0000-0000-000000000005");
            var identifier = new RoadSegmentId(value);

            Assert.Equal(
                expected: value,
                actual: identifier.Value);
        }

        [Fact]
        public void RoadSegmentId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new RoadSegmentId(Guid.Empty));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }
    }
}
