using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;

public sealed class TopologyRepositoryTests
{
    [Fact]
    public async Task DistrictRepository_AddRangeAndListByCityId_ReturnsOnlyCityDistricts()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(DistrictRepository_AddRangeAndListByCityId_ReturnsOnlyCityDistricts));
        City city = RepositoryTestData.CreateCity(name: "Topology City");
        City otherCity = RepositoryTestData.CreateCity(RepositoryTestData.BaseUtc.AddMinutes(1), "Other City");
        District district = RepositoryTestData.CreateDistrict(city.Id, "Beta");
        District other = RepositoryTestData.CreateDistrict(otherCity.Id, "Other");
        await dbContext.Cities.AddRangeAsync(city, otherCity);
        await dbContext.SaveChangesAsync();
        var repository = new DistrictRepository(dbContext);

        await repository.AddRangeAsync([district, other], CancellationToken.None);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        IReadOnlyList<District> result = await repository.ListByCityIdAsync(city.Id, CancellationToken.None);

        Assert.Equal([district.Id], result.Select(static x => x.Id).ToArray());
        Assert.Empty(dbContext.ChangeTracker.Entries<District>());
    }

    [Fact]
    public async Task CityAnchorRepository_AddRangeGetAndList_ReturnsMatchingAnchors()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(CityAnchorRepository_AddRangeGetAndList_ReturnsMatchingAnchors));
        City city = RepositoryTestData.CreateCity(name: "Anchor City");
        City otherCity = RepositoryTestData.CreateCity(RepositoryTestData.BaseUtc.AddMinutes(1), "Other Anchor City");
        District district = RepositoryTestData.CreateDistrict(city.Id, "Services");
        District otherDistrict = RepositoryTestData.CreateDistrict(otherCity.Id, "Other Services");
        RoadNode accessNode = RepositoryTestData.CreateRoadNode(city.Id, district.Id, "Access");
        RoadNode otherAccessNode = RepositoryTestData.CreateRoadNode(otherCity.Id, otherDistrict.Id, "Other Access");
        CityAnchor hospital = RepositoryTestData.CreateCityAnchor(city.Id, district.Id, accessNode.Id, "Hospital");
        CityAnchor university = RepositoryTestData.CreateCityAnchor(city.Id, district.Id, accessNode.Id, "University");
        CityAnchor other = RepositoryTestData.CreateCityAnchor(otherCity.Id, otherDistrict.Id, otherAccessNode.Id, "Other");
        await dbContext.Cities.AddRangeAsync(city, otherCity);
        await dbContext.Districts.AddRangeAsync(district, otherDistrict);
        await dbContext.RoadNodes.AddRangeAsync(accessNode, otherAccessNode);
        await dbContext.SaveChangesAsync();
        var repository = new CityAnchorRepository(dbContext);

        await repository.AddRangeAsync([hospital, university, other], CancellationToken.None);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        CityAnchor? fetched = await repository.GetByIdAsync(hospital.Id, CancellationToken.None);
        IReadOnlyList<CityAnchor> listed = await repository.ListByCityIdAsync(city.Id, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(hospital.Id, fetched.Id);
        Assert.Equal(
            new[] { hospital.Id.Value, university.Id.Value }.Order().ToArray(),
            listed.Select(static x => x.Id.Value).Order().ToArray());
        Assert.Empty(dbContext.ChangeTracker.Entries<CityAnchor>());
    }

    [Fact]
    public async Task ResidentialBuildingRepository_AddRangeGetAndList_AppliesCityAndDistrictFilters()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ResidentialBuildingRepository_AddRangeGetAndList_AppliesCityAndDistrictFilters));
        City city = RepositoryTestData.CreateCity(name: "Residential City");
        City otherCity = RepositoryTestData.CreateCity(RepositoryTestData.BaseUtc.AddMinutes(1), "Other Residential City");
        District north = RepositoryTestData.CreateDistrict(city.Id, "North");
        District south = RepositoryTestData.CreateDistrict(city.Id, "South");
        District otherDistrict = RepositoryTestData.CreateDistrict(otherCity.Id, "Other");
        RoadNode northNode = RepositoryTestData.CreateRoadNode(city.Id, north.Id, "North Node");
        RoadNode southNode = RepositoryTestData.CreateRoadNode(city.Id, south.Id, "South Node");
        RoadNode otherNode = RepositoryTestData.CreateRoadNode(otherCity.Id, otherDistrict.Id, "Other Node");
        ResidentialBuilding northTower = RepositoryTestData.CreateResidentialBuilding(city.Id, north.Id, northNode.Id, "North Tower");
        ResidentialBuilding southTower = RepositoryTestData.CreateResidentialBuilding(city.Id, south.Id, southNode.Id, "South Tower");
        ResidentialBuilding otherTower = RepositoryTestData.CreateResidentialBuilding(otherCity.Id, otherDistrict.Id, otherNode.Id, "Other Tower");
        await dbContext.Cities.AddRangeAsync(city, otherCity);
        await dbContext.Districts.AddRangeAsync(north, south, otherDistrict);
        await dbContext.RoadNodes.AddRangeAsync(northNode, southNode, otherNode);
        await dbContext.SaveChangesAsync();
        var repository = new ResidentialBuildingRepository(dbContext);

        await repository.AddRangeAsync([northTower, southTower, otherTower], CancellationToken.None);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        ResidentialBuilding? fetched = await repository.GetByIdAsync(northTower.Id, CancellationToken.None);
        IReadOnlyList<ResidentialBuilding> cityBuildings = await repository.ListByCityIdAsync(
            city.Id,
            districtId: null,
            cancellationToken: CancellationToken.None);
        IReadOnlyList<ResidentialBuilding> northBuildings = await repository.ListByCityIdAsync(
            city.Id,
            districtId: north.Id,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(northTower.Id, fetched.Id);
        Assert.Equal(
            new[] { northTower.Id.Value, southTower.Id.Value }.Order().ToArray(),
            cityBuildings.Select(static x => x.Id.Value).Order().ToArray());
        Assert.Equal([northTower.Id], northBuildings.Select(static x => x.Id).ToArray());
        Assert.Empty(dbContext.ChangeTracker.Entries<ResidentialBuilding>());
    }

    [Fact]
    public async Task RoadNodeRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadNodes()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(RoadNodeRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadNodes));
        City city = RepositoryTestData.CreateCity(name: "Node City");
        City otherCity = RepositoryTestData.CreateCity(RepositoryTestData.BaseUtc.AddMinutes(1), "Other Node City");
        District district = RepositoryTestData.CreateDistrict(city.Id, "Roads");
        District otherDistrict = RepositoryTestData.CreateDistrict(otherCity.Id, "Other Roads");
        RoadNode first = RepositoryTestData.CreateRoadNode(city.Id, district.Id, "First");
        RoadNode second = RepositoryTestData.CreateRoadNode(city.Id, district.Id, "Second");
        RoadNode other = RepositoryTestData.CreateRoadNode(otherCity.Id, otherDistrict.Id, "Other");
        await dbContext.Cities.AddRangeAsync(city, otherCity);
        await dbContext.Districts.AddRangeAsync(district, otherDistrict);
        await dbContext.SaveChangesAsync();
        var repository = new RoadNodeRepository(dbContext);

        await repository.AddRangeAsync([first, second, other], CancellationToken.None);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        IReadOnlyList<RoadNode> result = await repository.ListByCityIdAsync(city.Id, CancellationToken.None);

        Assert.Equal(
            new[] { first.Id.Value, second.Id.Value }.Order().ToArray(),
            result.Select(static x => x.Id.Value).Order().ToArray());
        Assert.Empty(dbContext.ChangeTracker.Entries<RoadNode>());
    }

    [Fact]
    public async Task RoadSegmentRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadSegments()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(RoadSegmentRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadSegments));
        City city = RepositoryTestData.CreateCity(name: "Segment City");
        City otherCity = RepositoryTestData.CreateCity(RepositoryTestData.BaseUtc.AddMinutes(1), "Other Segment City");
        District district = RepositoryTestData.CreateDistrict(city.Id, "Roads");
        District otherDistrict = RepositoryTestData.CreateDistrict(otherCity.Id, "Other Roads");
        RoadNode from = RepositoryTestData.CreateRoadNode(city.Id, district.Id, "From");
        RoadNode to = RepositoryTestData.CreateRoadNode(city.Id, district.Id, "To");
        RoadNode otherFrom = RepositoryTestData.CreateRoadNode(otherCity.Id, otherDistrict.Id, "Other From");
        RoadNode otherTo = RepositoryTestData.CreateRoadNode(otherCity.Id, otherDistrict.Id, "Other To");
        RoadSegment first = RepositoryTestData.CreateRoadSegment(city.Id, district.Id, from.Id, to.Id, "First");
        RoadSegment second = RepositoryTestData.CreateRoadSegment(city.Id, district.Id, to.Id, from.Id, "Second");
        RoadSegment other = RepositoryTestData.CreateRoadSegment(otherCity.Id, otherDistrict.Id, otherFrom.Id, otherTo.Id, "Other");
        await dbContext.Cities.AddRangeAsync(city, otherCity);
        await dbContext.Districts.AddRangeAsync(district, otherDistrict);
        await dbContext.RoadNodes.AddRangeAsync(from, to, otherFrom, otherTo);
        await dbContext.SaveChangesAsync();
        var repository = new RoadSegmentRepository(dbContext);

        await repository.AddRangeAsync([first, second, other], CancellationToken.None);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        IReadOnlyList<RoadSegment> result = await repository.ListByCityIdAsync(city.Id, CancellationToken.None);

        Assert.Equal(
            new[] { first.Id.Value, second.Id.Value }.Order().ToArray(),
            result.Select(static x => x.Id.Value).Order().ToArray());
        Assert.Empty(dbContext.ChangeTracker.Entries<RoadSegment>());
    }
}
