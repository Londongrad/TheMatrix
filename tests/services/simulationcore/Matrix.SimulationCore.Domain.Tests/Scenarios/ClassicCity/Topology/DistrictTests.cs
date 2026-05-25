using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class DistrictTests
    {
        [Fact]
        public void Create_WithValidValues_SetsProperties_AndNormalizesCoordinates()
        {
            District district = TopologyTestData.CreateDistrict();

            Assert.Equal(
                expected: TopologyTestData.CityId,
                actual: district.CityId);
            Assert.Equal(
                expected: new DistrictName("Downtown"),
                actual: district.Name);
            Assert.Equal(
                expected: 12.346m,
                actual: district.AnchorX);
            Assert.Equal(
                expected: 45.678m,
                actual: district.AnchorY);
            Assert.Equal(
                expected: TopologyTestData.CreatedAtUtc,
                actual: district.CreatedAtUtc);
            Assert.Empty(district.DomainEvents);
        }

        [Fact]
        public void Create_WithNonUtcTimestamp_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => District.Create(
                cityId: TopologyTestData.CityId,
                name: new DistrictName("Downtown"),
                anchorX: 10m,
                anchorY: 20m,
                createdAtUtc: TopologyTestData.NonUtcCreatedAt));

            Assert.Equal(
                expected: "SimulationCore.Topology.Timestamp.NotUtc",
                actual: exception.Code);
            Assert.Equal(
                expected: "value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Rename_WhenNameChanges_UpdatesName()
        {
            District district = TopologyTestData.CreateDistrict();

            district.Rename(new DistrictName("Riverside"));

            Assert.Equal(
                expected: new DistrictName("Riverside"),
                actual: district.Name);
        }

        [Fact]
        public void Rename_WithSameName_IsNoOp()
        {
            District district = TopologyTestData.CreateDistrict();

            district.Rename(new DistrictName("Downtown"));

            Assert.Equal(
                expected: new DistrictName("Downtown"),
                actual: district.Name);
        }
    }
}
