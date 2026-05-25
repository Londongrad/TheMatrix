using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Topology
{
    public sealed class CityTopologyBootstrapFactoryTests
    {
        [Fact]
        public void CreateInitial_WithSameCity_ProducesDeterministicTopologyShape()
        {
            City city = ClassicCityTestSupport.CreateCity();
            CityTopologyBootstrapFactory factory = CreateFactory(
                districtNames:
                [
                    "North District",
                    "Harbor District",
                    "University District",
                    "Garden District"
                ],
                streetNames:
                [
                    "Atlas Avenue",
                    "Mercury Boulevard",
                    "Signal Road"
                ]);

            CityTopologySeed first = factory.CreateInitial(city);
            CityTopologySeed second = factory.CreateInitial(city);

            Assert.Equal(
                expected: first.Districts.Select(static x => (x.Name.Value, x.AnchorX, x.AnchorY))
                   .ToArray(),
                actual: second.Districts.Select(static x => (x.Name.Value, x.AnchorX, x.AnchorY))
                   .ToArray());
            Assert.Equal(
                expected: first.ResidentialBuildings.Select(static x
                        => (x.Name.Value, x.Type, x.ResidentCapacity.Value, x.PositionX, x.PositionY))
                   .ToArray(),
                actual: second.ResidentialBuildings.Select(static x
                        => (x.Name.Value, x.Type, x.ResidentCapacity.Value, x.PositionX, x.PositionY))
                   .ToArray());
            Assert.Equal(
                expected: first.Anchors.Select(static x => (x.Name.Value, x.Type, x.Capacity, x.PositionX, x.PositionY))
                   .ToArray(),
                actual: second.Anchors.Select(static x => (x.Name.Value, x.Type, x.Capacity, x.PositionX, x.PositionY))
                   .ToArray());
            Assert.Equal(
                expected: first.RoadNodes.Select(static x => (x.Name, x.Type, x.PositionX, x.PositionY))
                   .ToArray(),
                actual: second.RoadNodes.Select(static x => (x.Name, x.Type, x.PositionX, x.PositionY))
                   .ToArray());
            Assert.Equal(
                expected: first.RoadSegments.Select(static x => (x.Name, x.Type, x.LengthMeters))
                   .ToArray(),
                actual: second.RoadSegments.Select(static x => (x.Name, x.Type, x.LengthMeters))
                   .ToArray());
        }

        [Fact]
        public void CreateInitial_WhenCatalogPresetsAreSparse_UsesFallbackDistrictAndConnectorNames()
        {
            City city = ClassicCityTestSupport.CreateCity();
            CityTopologyBootstrapFactory factory = CreateFactory(
                districtNames: Array.Empty<string>(),
                streetNames: Array.Empty<string>());

            CityTopologySeed topology = factory.CreateInitial(city);

            Assert.Contains(
                collection: topology.Districts,
                filter: static x => x.Name.Value == "Central District");
            Assert.Contains(
                collection: topology.Districts,
                filter: static x => x.Name.Value == "Sector 1");
            Assert.Contains(
                collection: topology.RoadSegments,
                filter: static x => x.Name.EndsWith(
                    value: "Connector",
                    comparisonType: StringComparison.Ordinal));
        }

        [Fact]
        public void CreateInitial_ProducesInternallyConsistentTopologyReferences()
        {
            City city = ClassicCityTestSupport.CreateCity();
            CityTopologyBootstrapFactory factory = CreateFactory(
                districtNames:
                [
                    "North District",
                    "Harbor District",
                    "University District",
                    "Garden District"
                ],
                streetNames:
                [
                    "Atlas Avenue",
                    "Mercury Boulevard",
                    "Signal Road"
                ]);

            CityTopologySeed topology = factory.CreateInitial(city);

            Assert.NotEmpty(topology.Districts);
            Assert.NotEmpty(topology.ResidentialBuildings);
            Assert.NotEmpty(topology.Anchors);
            Assert.NotEmpty(topology.RoadNodes);
            Assert.NotEmpty(topology.RoadSegments);

            var districtIds = topology.Districts.Select(static x => x.Id)
               .ToHashSet();
            var roadNodeIds = topology.RoadNodes.Select(static x => x.Id)
               .ToHashSet();

            Assert.All(
                collection: topology.Districts,
                action: x => Assert.Equal(
                    expected: city.Id,
                    actual: x.CityId));
            Assert.All(
                collection: topology.ResidentialBuildings,
                action: x =>
                {
                    Assert.Equal(
                        expected: city.Id,
                        actual: x.CityId);
                    Assert.Contains(
                        expected: x.DistrictId,
                        set: districtIds);
                    Assert.Contains(
                        expected: x.AccessRoadNodeId,
                        set: roadNodeIds);
                });
            Assert.All(
                collection: topology.Anchors,
                action: x =>
                {
                    Assert.Equal(
                        expected: city.Id,
                        actual: x.CityId);
                    Assert.Contains(
                        expected: x.DistrictId,
                        set: districtIds);
                    Assert.Contains(
                        expected: x.AccessRoadNodeId,
                        set: roadNodeIds);
                });
            Assert.All(
                collection: topology.RoadNodes,
                action: x =>
                {
                    Assert.Equal(
                        expected: city.Id,
                        actual: x.CityId);
                    Assert.Contains(
                        expected: x.DistrictId,
                        set: districtIds);
                });
            Assert.All(
                collection: topology.RoadSegments,
                action: x =>
                {
                    Assert.Equal(
                        expected: city.Id,
                        actual: x.CityId);
                    Assert.Contains(
                        expected: x.DistrictId,
                        set: districtIds);
                    Assert.Contains(
                        expected: x.FromRoadNodeId,
                        set: roadNodeIds);
                    Assert.Contains(
                        expected: x.ToRoadNodeId,
                        set: roadNodeIds);
                    Assert.NotEqual(
                        expected: x.FromRoadNodeId,
                        actual: x.ToRoadNodeId);
                });
        }

        private static CityTopologyBootstrapFactory CreateFactory(
            IReadOnlyList<string> districtNames,
            IReadOnlyList<string> streetNames)
        {
            return new CityTopologyBootstrapFactory(
                new ClassicCityTestSupport.FakeCityGenerationContentCatalog
                {
                    DistrictNamePresets = districtNames,
                    StreetNamePresets = streetNames
                });
        }
    }
}
