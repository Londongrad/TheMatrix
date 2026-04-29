using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Topology;

public sealed class CityTopologyBootstrapFactoryTests
{
    [Fact]
    public void CreateInitial_WithSameCity_ProducesDeterministicTopologyShape()
    {
        City city = ClassicCityTestSupport.CreateCity();
        var factory = CreateFactory(
            districtNames: ["North District", "Harbor District", "University District", "Garden District"],
            streetNames: ["Atlas Avenue", "Mercury Boulevard", "Signal Road"]);

        CityTopologySeed first = factory.CreateInitial(city);
        CityTopologySeed second = factory.CreateInitial(city);

        Assert.Equal(
            first.Districts.Select(static x => (x.Name.Value, x.AnchorX, x.AnchorY)).ToArray(),
            second.Districts.Select(static x => (x.Name.Value, x.AnchorX, x.AnchorY)).ToArray());
        Assert.Equal(
            first.ResidentialBuildings.Select(static x => (x.Name.Value, x.Type, x.ResidentCapacity.Value, x.PositionX, x.PositionY)).ToArray(),
            second.ResidentialBuildings.Select(static x => (x.Name.Value, x.Type, x.ResidentCapacity.Value, x.PositionX, x.PositionY)).ToArray());
        Assert.Equal(
            first.Anchors.Select(static x => (x.Name.Value, x.Type, x.Capacity, x.PositionX, x.PositionY)).ToArray(),
            second.Anchors.Select(static x => (x.Name.Value, x.Type, x.Capacity, x.PositionX, x.PositionY)).ToArray());
        Assert.Equal(
            first.RoadNodes.Select(static x => (x.Name, x.Type, x.PositionX, x.PositionY)).ToArray(),
            second.RoadNodes.Select(static x => (x.Name, x.Type, x.PositionX, x.PositionY)).ToArray());
        Assert.Equal(
            first.RoadSegments.Select(static x => (x.Name, x.Type, x.LengthMeters)).ToArray(),
            second.RoadSegments.Select(static x => (x.Name, x.Type, x.LengthMeters)).ToArray());
    }

    [Fact]
    public void CreateInitial_WhenCatalogPresetsAreSparse_UsesFallbackDistrictAndConnectorNames()
    {
        City city = ClassicCityTestSupport.CreateCity();
        var factory = CreateFactory(
            districtNames: Array.Empty<string>(),
            streetNames: Array.Empty<string>());

        CityTopologySeed topology = factory.CreateInitial(city);

        Assert.Contains(topology.Districts, static x => x.Name.Value == "Central District");
        Assert.Contains(topology.Districts, static x => x.Name.Value == "Sector 1");
        Assert.Contains(topology.RoadSegments, static x => x.Name.EndsWith("Connector", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateInitial_ProducesInternallyConsistentTopologyReferences()
    {
        City city = ClassicCityTestSupport.CreateCity();
        var factory = CreateFactory(
            districtNames: ["North District", "Harbor District", "University District", "Garden District"],
            streetNames: ["Atlas Avenue", "Mercury Boulevard", "Signal Road"]);

        CityTopologySeed topology = factory.CreateInitial(city);

        Assert.NotEmpty(topology.Districts);
        Assert.NotEmpty(topology.ResidentialBuildings);
        Assert.NotEmpty(topology.Anchors);
        Assert.NotEmpty(topology.RoadNodes);
        Assert.NotEmpty(topology.RoadSegments);

        HashSet<DistrictId> districtIds = topology.Districts.Select(static x => x.Id).ToHashSet();
        HashSet<RoadNodeId> roadNodeIds = topology.RoadNodes.Select(static x => x.Id).ToHashSet();

        Assert.All(topology.Districts, x => Assert.Equal(city.Id, x.CityId));
        Assert.All(topology.ResidentialBuildings, x =>
        {
            Assert.Equal(city.Id, x.CityId);
            Assert.Contains(x.DistrictId, districtIds);
            Assert.Contains(x.AccessRoadNodeId, roadNodeIds);
        });
        Assert.All(topology.Anchors, x =>
        {
            Assert.Equal(city.Id, x.CityId);
            Assert.Contains(x.DistrictId, districtIds);
            Assert.Contains(x.AccessRoadNodeId, roadNodeIds);
        });
        Assert.All(topology.RoadNodes, x =>
        {
            Assert.Equal(city.Id, x.CityId);
            Assert.Contains(x.DistrictId, districtIds);
        });
        Assert.All(topology.RoadSegments, x =>
        {
            Assert.Equal(city.Id, x.CityId);
            Assert.Contains(x.DistrictId, districtIds);
            Assert.Contains(x.FromRoadNodeId, roadNodeIds);
            Assert.Contains(x.ToRoadNodeId, roadNodeIds);
            Assert.NotEqual(x.FromRoadNodeId, x.ToRoadNodeId);
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
