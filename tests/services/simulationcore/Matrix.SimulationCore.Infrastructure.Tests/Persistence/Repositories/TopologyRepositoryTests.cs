using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class TopologyRepositoryTests
    {
        [Fact]
        public async Task DistrictRepository_AddRangeAndListByCityId_ReturnsOnlyCityDistricts()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(DistrictRepository_AddRangeAndListByCityId_ReturnsOnlyCityDistricts));
            City city = RepositoryTestData.CreateCity(name: "Topology City");
            City otherCity = RepositoryTestData.CreateCity(
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1),
                name: "Other City");
            District district = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "Beta");
            District other = RepositoryTestData.CreateDistrict(
                cityId: otherCity.Id,
                name: "Other");
            await dbContext.Cities.AddRangeAsync(
                city,
                otherCity);
            await dbContext.SaveChangesAsync();
            var repository = new DistrictRepository(dbContext);

            await repository.AddRangeAsync(
                districts:
                [
                    district,
                    other
                ],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<District> result = await repository.ListByCityIdAsync(
                cityId: city.Id,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expectedSpan: [district.Id],
                actualArray: result.Select(static x => x.Id)
                   .ToArray());
            Assert.Empty(dbContext.ChangeTracker.Entries<District>());
        }

        [Fact]
        public async Task CityAnchorRepository_AddRangeGetAndList_ReturnsMatchingAnchors()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(CityAnchorRepository_AddRangeGetAndList_ReturnsMatchingAnchors));
            City city = RepositoryTestData.CreateCity(name: "Anchor City");
            City otherCity = RepositoryTestData.CreateCity(
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1),
                name: "Other Anchor City");
            District district = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "Services");
            District otherDistrict = RepositoryTestData.CreateDistrict(
                cityId: otherCity.Id,
                name: "Other Services");
            RoadNode accessNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "Access");
            RoadNode otherAccessNode = RepositoryTestData.CreateRoadNode(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                name: "Other Access");
            CityAnchor hospital = RepositoryTestData.CreateCityAnchor(
                cityId: city.Id,
                districtId: district.Id,
                accessRoadNodeId: accessNode.Id,
                name: "Hospital");
            CityAnchor university = RepositoryTestData.CreateCityAnchor(
                cityId: city.Id,
                districtId: district.Id,
                accessRoadNodeId: accessNode.Id,
                name: "University");
            CityAnchor other = RepositoryTestData.CreateCityAnchor(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                accessRoadNodeId: otherAccessNode.Id,
                name: "Other");
            await dbContext.Cities.AddRangeAsync(
                city,
                otherCity);
            await dbContext.Districts.AddRangeAsync(
                district,
                otherDistrict);
            await dbContext.RoadNodes.AddRangeAsync(
                accessNode,
                otherAccessNode);
            await dbContext.SaveChangesAsync();
            var repository = new CityAnchorRepository(dbContext);

            await repository.AddRangeAsync(
                anchors:
                [
                    hospital,
                    university,
                    other
                ],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            CityAnchor? fetched = await repository.GetByIdAsync(
                anchorId: hospital.Id,
                cancellationToken: CancellationToken.None);
            IReadOnlyList<CityAnchor> listed = await repository.ListByCityIdAsync(
                cityId: city.Id,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(fetched);
            Assert.Equal(
                expected: hospital.Id,
                actual: fetched.Id);
            Assert.Equal(
                expected: new[]
                    {
                        hospital.Id.Value,
                        university.Id.Value
                    }.Order()
                   .ToArray(),
                actual: listed.Select(static x => x.Id.Value)
                   .Order()
                   .ToArray());
            Assert.Empty(dbContext.ChangeTracker.Entries<CityAnchor>());
        }

        [Fact]
        public async Task ResidentialBuildingRepository_AddRangeGetAndList_AppliesCityAndDistrictFilters()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ResidentialBuildingRepository_AddRangeGetAndList_AppliesCityAndDistrictFilters));
            City city = RepositoryTestData.CreateCity(name: "Residential City");
            City otherCity = RepositoryTestData.CreateCity(
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1),
                name: "Other Residential City");
            District north = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "North");
            District south = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "South");
            District otherDistrict = RepositoryTestData.CreateDistrict(
                cityId: otherCity.Id,
                name: "Other");
            RoadNode northNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: north.Id,
                name: "North Node");
            RoadNode southNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: south.Id,
                name: "South Node");
            RoadNode otherNode = RepositoryTestData.CreateRoadNode(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                name: "Other Node");
            ResidentialBuilding northTower = RepositoryTestData.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: north.Id,
                accessRoadNodeId: northNode.Id,
                name: "North Tower");
            ResidentialBuilding southTower = RepositoryTestData.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: south.Id,
                accessRoadNodeId: southNode.Id,
                name: "South Tower");
            ResidentialBuilding otherTower = RepositoryTestData.CreateResidentialBuilding(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                accessRoadNodeId: otherNode.Id,
                name: "Other Tower");
            await dbContext.Cities.AddRangeAsync(
                city,
                otherCity);
            await dbContext.Districts.AddRangeAsync(
                north,
                south,
                otherDistrict);
            await dbContext.RoadNodes.AddRangeAsync(
                northNode,
                southNode,
                otherNode);
            await dbContext.SaveChangesAsync();
            var repository = new ResidentialBuildingRepository(dbContext);

            await repository.AddRangeAsync(
                buildings:
                [
                    northTower,
                    southTower,
                    otherTower
                ],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            ResidentialBuilding? fetched = await repository.GetByIdAsync(
                buildingId: northTower.Id,
                cancellationToken: CancellationToken.None);
            IReadOnlyList<ResidentialBuilding> cityBuildings = await repository.ListByCityIdAsync(
                cityId: city.Id,
                districtId: null,
                cancellationToken: CancellationToken.None);
            IReadOnlyList<ResidentialBuilding> northBuildings = await repository.ListByCityIdAsync(
                cityId: city.Id,
                districtId: north.Id,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(fetched);
            Assert.Equal(
                expected: northTower.Id,
                actual: fetched.Id);
            Assert.Equal(
                expected: new[]
                    {
                        northTower.Id.Value,
                        southTower.Id.Value
                    }.Order()
                   .ToArray(),
                actual: cityBuildings.Select(static x => x.Id.Value)
                   .Order()
                   .ToArray());
            Assert.Equal(
                expectedSpan: [northTower.Id],
                actualArray: northBuildings.Select(static x => x.Id)
                   .ToArray());
            Assert.Empty(dbContext.ChangeTracker.Entries<ResidentialBuilding>());
        }

        [Fact]
        public async Task RoadNodeRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadNodes()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(RoadNodeRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadNodes));
            City city = RepositoryTestData.CreateCity(name: "Node City");
            City otherCity = RepositoryTestData.CreateCity(
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1),
                name: "Other Node City");
            District district = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "Roads");
            District otherDistrict = RepositoryTestData.CreateDistrict(
                cityId: otherCity.Id,
                name: "Other Roads");
            RoadNode first = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "First");
            RoadNode second = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "Second");
            RoadNode other = RepositoryTestData.CreateRoadNode(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                name: "Other");
            await dbContext.Cities.AddRangeAsync(
                city,
                otherCity);
            await dbContext.Districts.AddRangeAsync(
                district,
                otherDistrict);
            await dbContext.SaveChangesAsync();
            var repository = new RoadNodeRepository(dbContext);

            await repository.AddRangeAsync(
                roadNodes:
                [
                    first,
                    second,
                    other
                ],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<RoadNode> result = await repository.ListByCityIdAsync(
                cityId: city.Id,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new[]
                    {
                        first.Id.Value,
                        second.Id.Value
                    }.Order()
                   .ToArray(),
                actual: result.Select(static x => x.Id.Value)
                   .Order()
                   .ToArray());
            Assert.Empty(dbContext.ChangeTracker.Entries<RoadNode>());
        }

        [Fact]
        public async Task RoadSegmentRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadSegments()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(RoadSegmentRepository_AddRangeAndListByCityId_ReturnsOnlyCityRoadSegments));
            City city = RepositoryTestData.CreateCity(name: "Segment City");
            City otherCity = RepositoryTestData.CreateCity(
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1),
                name: "Other Segment City");
            District district = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "Roads");
            District otherDistrict = RepositoryTestData.CreateDistrict(
                cityId: otherCity.Id,
                name: "Other Roads");
            RoadNode from = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "From");
            RoadNode to = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "To");
            RoadNode otherFrom = RepositoryTestData.CreateRoadNode(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                name: "Other From");
            RoadNode otherTo = RepositoryTestData.CreateRoadNode(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                name: "Other To");
            RoadSegment first = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: district.Id,
                fromRoadNodeId: from.Id,
                toRoadNodeId: to.Id,
                name: "First");
            RoadSegment second = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: district.Id,
                fromRoadNodeId: to.Id,
                toRoadNodeId: from.Id,
                name: "Second");
            RoadSegment other = RepositoryTestData.CreateRoadSegment(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                fromRoadNodeId: otherFrom.Id,
                toRoadNodeId: otherTo.Id,
                name: "Other");
            await dbContext.Cities.AddRangeAsync(
                city,
                otherCity);
            await dbContext.Districts.AddRangeAsync(
                district,
                otherDistrict);
            await dbContext.RoadNodes.AddRangeAsync(
                from,
                to,
                otherFrom,
                otherTo);
            await dbContext.SaveChangesAsync();
            var repository = new RoadSegmentRepository(dbContext);

            await repository.AddRangeAsync(
                roadSegments:
                [
                    first,
                    second,
                    other
                ],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<RoadSegment> result = await repository.ListByCityIdAsync(
                cityId: city.Id,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new[]
                    {
                        first.Id.Value,
                        second.Id.Value
                    }.Order()
                   .ToArray(),
                actual: result.Select(static x => x.Id.Value)
                   .Order()
                   .ToArray());
            Assert.Empty(dbContext.ChangeTracker.Entries<RoadSegment>());
        }
    }
}
