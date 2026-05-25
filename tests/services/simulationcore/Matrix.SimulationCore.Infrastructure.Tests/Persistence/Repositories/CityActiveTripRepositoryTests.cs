using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityActiveTripRepositoryTests
    {
        [Fact]
        public async Task ListActiveByCityIdAsync_ReturnsOnlyActiveTripsSortedDescendingAndUntracked()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ListActiveByCityIdAsync_ReturnsOnlyActiveTripsSortedDescendingAndUntracked));
            City city = RepositoryTestData.CreateCity();
            City otherCity = RepositoryTestData.CreateCity(
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1),
                name: "Other City");
            District fromDistrict = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "From");
            District toDistrict = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "To",
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1));
            District otherDistrict = RepositoryTestData.CreateDistrict(
                cityId: otherCity.Id,
                name: "Other");
            RoadNode fromNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                name: "From Node");
            RoadNode midNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                name: "Mid Node",
                positionX: 20m);
            RoadNode toNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: toDistrict.Id,
                name: "To Node",
                positionX: 30m);
            RoadNode otherNode = RepositoryTestData.CreateRoadNode(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                name: "Other Node");
            RoadSegment firstSegment = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                toRoadNodeId: midNode.Id,
                name: "First");
            RoadSegment secondSegment = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: toDistrict.Id,
                fromRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                name: "Second");
            RoadSegment otherSegment = RepositoryTestData.CreateRoadSegment(
                cityId: otherCity.Id,
                districtId: otherDistrict.Id,
                fromRoadNodeId: otherNode.Id,
                toRoadNodeId: RoadNodeId.New(),
                name: "Other Segment");

            CityActiveTrip activeOlder = RepositoryTestData.CreateTrip(
                cityId: city.Id,
                fromDistrictId: fromDistrict.Id,
                toDistrictId: toDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                midRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                firstRoadSegmentId: firstSegment.Id,
                secondRoadSegmentId: secondSegment.Id,
                startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(3));
            CityActiveTrip activeNewer = RepositoryTestData.CreateTrip(
                cityId: city.Id,
                fromDistrictId: fromDistrict.Id,
                toDistrictId: toDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                midRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                firstRoadSegmentId: firstSegment.Id,
                secondRoadSegmentId: secondSegment.Id,
                startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(4));
            CityActiveTrip arrivedTrip = RepositoryTestData.CreateTrip(
                cityId: city.Id,
                fromDistrictId: fromDistrict.Id,
                toDistrictId: toDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                midRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                firstRoadSegmentId: firstSegment.Id,
                secondRoadSegmentId: secondSegment.Id,
                startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(2));
            arrivedTrip.AdvanceTo(
                toSimTimeUtc: arrivedTrip.ExpectedArrivalAtSimTimeUtc.AddMinutes(1),
                tickId: 44);

            CityActiveTrip otherCityTrip = RepositoryTestData.CreateTrip(
                cityId: otherCity.Id,
                fromDistrictId: otherDistrict.Id,
                toDistrictId: otherDistrict.Id,
                fromRoadNodeId: otherNode.Id,
                midRoadNodeId: otherNode.Id,
                toRoadNodeId: otherNode.Id,
                firstRoadSegmentId: otherSegment.Id,
                secondRoadSegmentId: otherSegment.Id,
                startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(5));

            await dbContext.Cities.AddRangeAsync(
                city,
                otherCity);
            await dbContext.Districts.AddRangeAsync(
                fromDistrict,
                toDistrict,
                otherDistrict);
            await dbContext.RoadNodes.AddRangeAsync(
                fromNode,
                midNode,
                toNode,
                otherNode);
            await dbContext.RoadSegments.AddRangeAsync(
                firstSegment,
                secondSegment,
                otherSegment);
            await dbContext.Set<CityActiveTrip>()
               .AddRangeAsync(
                    activeOlder,
                    activeNewer,
                    arrivedTrip,
                    otherCityTrip);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            var repository = new CityActiveTripRepository(dbContext);

            IReadOnlyList<CityActiveTrip> result = await repository.ListActiveByCityIdAsync(
                cityId: city.Id,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expectedSpan:
                [
                    activeNewer.Id,
                    activeOlder.Id
                ],
                actualArray: result.Select(static x => x.Id)
                   .ToArray());
            Assert.All(
                collection: result,
                action: static trip => Assert.Single(trip.Segments));
            Assert.Empty(dbContext.ChangeTracker.Entries<CityActiveTrip>());
        }

        [Fact]
        public async Task ListActiveForUpdateByCityId_ReturnsTrackedTripsSortedAscending()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ListActiveForUpdateByCityId_ReturnsTrackedTripsSortedAscending));
            City city = RepositoryTestData.CreateCity();
            District fromDistrict = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "From");
            District toDistrict = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "To",
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1));
            RoadNode fromNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                name: "From Node");
            RoadNode midNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                name: "Mid Node",
                positionX: 20m);
            RoadNode toNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: toDistrict.Id,
                name: "To Node",
                positionX: 30m);
            RoadSegment firstSegment = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                toRoadNodeId: midNode.Id,
                name: "First");
            RoadSegment secondSegment = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: toDistrict.Id,
                fromRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                name: "Second");

            CityActiveTrip older = RepositoryTestData.CreateTrip(
                cityId: city.Id,
                fromDistrictId: fromDistrict.Id,
                toDistrictId: toDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                midRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                firstRoadSegmentId: firstSegment.Id,
                secondRoadSegmentId: secondSegment.Id,
                startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(3));
            CityActiveTrip newer = RepositoryTestData.CreateTrip(
                cityId: city.Id,
                fromDistrictId: fromDistrict.Id,
                toDistrictId: toDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                midRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                firstRoadSegmentId: firstSegment.Id,
                secondRoadSegmentId: secondSegment.Id,
                startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(4));

            await dbContext.Cities.AddAsync(city);
            await dbContext.Districts.AddRangeAsync(
                fromDistrict,
                toDistrict);
            await dbContext.RoadNodes.AddRangeAsync(
                fromNode,
                midNode,
                toNode);
            await dbContext.RoadSegments.AddRangeAsync(
                firstSegment,
                secondSegment);
            await dbContext.Set<CityActiveTrip>()
               .AddRangeAsync(
                    older,
                    newer);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            var repository = new CityActiveTripRepository(dbContext);

            IReadOnlyList<CityActiveTrip> result = await repository.ListActiveForUpdateByCityIdAsync(
                cityId: city.Id,
                cancellationToken: CancellationToken.None);
            result[0]
               .AdvanceTo(
                    toSimTimeUtc: result[0]
                       .StartedAtSimTimeUtc.AddMinutes(2),
                    tickId: 99);
            await dbContext.SaveChangesAsync();

            CityActiveTrip persistedOlder = await dbContext.Set<CityActiveTrip>()
               .AsNoTracking()
               .SingleAsync(x => x.Id == older.Id);

            Assert.Equal(
                expectedSpan:
                [
                    older.Id,
                    newer.Id
                ],
                actualArray: result.Select(static x => x.Id)
                   .ToArray());
            Assert.All(
                collection: result,
                action: static trip => Assert.Single(trip.Segments));
            Assert.Equal(
                expected: 2,
                actual: dbContext.ChangeTracker.Entries<CityActiveTrip>()
                   .Count());
            Assert.Equal(
                expected: 99,
                actual: persistedOlder.LastAdvancedTickId);
        }

        [Fact]
        public async Task AddAsync_WhenTripIsAdded_PersistsTripAndOwnedSegments()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(AddAsync_WhenTripIsAdded_PersistsTripAndOwnedSegments));
            City city = RepositoryTestData.CreateCity();
            District fromDistrict = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "From");
            District toDistrict = RepositoryTestData.CreateDistrict(
                cityId: city.Id,
                name: "To",
                createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1));
            RoadNode fromNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                name: "From Node");
            RoadNode midNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                name: "Mid Node",
                positionX: 20m);
            RoadNode toNode = RepositoryTestData.CreateRoadNode(
                cityId: city.Id,
                districtId: toDistrict.Id,
                name: "To Node",
                positionX: 30m);
            RoadSegment firstSegment = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: fromDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                toRoadNodeId: midNode.Id,
                name: "First");
            RoadSegment secondSegment = RepositoryTestData.CreateRoadSegment(
                cityId: city.Id,
                districtId: toDistrict.Id,
                fromRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                name: "Second");
            CityActiveTrip trip = RepositoryTestData.CreateTrip(
                cityId: city.Id,
                fromDistrictId: fromDistrict.Id,
                toDistrictId: toDistrict.Id,
                fromRoadNodeId: fromNode.Id,
                midRoadNodeId: midNode.Id,
                toRoadNodeId: toNode.Id,
                firstRoadSegmentId: firstSegment.Id,
                secondRoadSegmentId: secondSegment.Id);

            await dbContext.Cities.AddAsync(city);
            await dbContext.Districts.AddRangeAsync(
                fromDistrict,
                toDistrict);
            await dbContext.RoadNodes.AddRangeAsync(
                fromNode,
                midNode,
                toNode);
            await dbContext.RoadSegments.AddRangeAsync(
                firstSegment,
                secondSegment);
            await dbContext.SaveChangesAsync();
            var repository = new CityActiveTripRepository(dbContext);

            await repository.AddAsync(
                trip: trip,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            CityActiveTrip persistedTrip = await dbContext.Set<CityActiveTrip>()
               .AsNoTracking()
               .Include(x => x.Segments)
               .SingleAsync(x => x.Id == trip.Id);

            Assert.Equal(
                expected: trip.CityId,
                actual: persistedTrip.CityId);
            CityActiveTripSegment segment = Assert.Single(persistedTrip.Segments);
            Assert.Equal(
                expected: firstSegment.Id,
                actual: segment.RoadSegmentId);
        }
    }
}
