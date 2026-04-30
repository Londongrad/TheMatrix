using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityActiveTripRepositoryTests
{
    [Fact]
    public async Task ListActiveByCityIdAsync_ReturnsOnlyActiveTripsSortedDescendingAndUntracked()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ListActiveByCityIdAsync_ReturnsOnlyActiveTripsSortedDescendingAndUntracked));
        City city = RepositoryTestData.CreateCity();
        City otherCity = RepositoryTestData.CreateCity(RepositoryTestData.BaseUtc.AddMinutes(1), "Other City");
        District fromDistrict = RepositoryTestData.CreateDistrict(city.Id, "From");
        District toDistrict = RepositoryTestData.CreateDistrict(city.Id, "To", createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1));
        District otherDistrict = RepositoryTestData.CreateDistrict(otherCity.Id, "Other");
        RoadNode fromNode = RepositoryTestData.CreateRoadNode(city.Id, fromDistrict.Id, "From Node");
        RoadNode midNode = RepositoryTestData.CreateRoadNode(city.Id, fromDistrict.Id, "Mid Node", positionX: 20m);
        RoadNode toNode = RepositoryTestData.CreateRoadNode(city.Id, toDistrict.Id, "To Node", positionX: 30m);
        RoadNode otherNode = RepositoryTestData.CreateRoadNode(otherCity.Id, otherDistrict.Id, "Other Node");
        RoadSegment firstSegment = RepositoryTestData.CreateRoadSegment(city.Id, fromDistrict.Id, fromNode.Id, midNode.Id, "First");
        RoadSegment secondSegment = RepositoryTestData.CreateRoadSegment(city.Id, toDistrict.Id, midNode.Id, toNode.Id, "Second");
        RoadSegment otherSegment = RepositoryTestData.CreateRoadSegment(otherCity.Id, otherDistrict.Id, otherNode.Id, RoadNodeId.New(), "Other Segment");

        CityActiveTrip activeOlder = RepositoryTestData.CreateTrip(
            city.Id,
            fromDistrict.Id,
            toDistrict.Id,
            fromNode.Id,
            midNode.Id,
            toNode.Id,
            firstSegment.Id,
            secondSegment.Id,
            startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(3));
        CityActiveTrip activeNewer = RepositoryTestData.CreateTrip(
            city.Id,
            fromDistrict.Id,
            toDistrict.Id,
            fromNode.Id,
            midNode.Id,
            toNode.Id,
            firstSegment.Id,
            secondSegment.Id,
            startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(4));
        CityActiveTrip arrivedTrip = RepositoryTestData.CreateTrip(
            city.Id,
            fromDistrict.Id,
            toDistrict.Id,
            fromNode.Id,
            midNode.Id,
            toNode.Id,
            firstSegment.Id,
            secondSegment.Id,
            startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(2));
        arrivedTrip.AdvanceTo(arrivedTrip.ExpectedArrivalAtSimTimeUtc.AddMinutes(1), 44);

        CityActiveTrip otherCityTrip = RepositoryTestData.CreateTrip(
            otherCity.Id,
            otherDistrict.Id,
            otherDistrict.Id,
            otherNode.Id,
            otherNode.Id,
            otherNode.Id,
            otherSegment.Id,
            otherSegment.Id,
            startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(5));

        await dbContext.Cities.AddRangeAsync(city, otherCity);
        await dbContext.Districts.AddRangeAsync(fromDistrict, toDistrict, otherDistrict);
        await dbContext.RoadNodes.AddRangeAsync(fromNode, midNode, toNode, otherNode);
        await dbContext.RoadSegments.AddRangeAsync(firstSegment, secondSegment, otherSegment);
        await dbContext.Set<CityActiveTrip>().AddRangeAsync(activeOlder, activeNewer, arrivedTrip, otherCityTrip);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new CityActiveTripRepository(dbContext);

        IReadOnlyList<CityActiveTrip> result = await repository.ListActiveByCityIdAsync(city.Id, CancellationToken.None);

        Assert.Equal([activeNewer.Id, activeOlder.Id], result.Select(static x => x.Id).ToArray());
        Assert.All(result, static trip => Assert.Single(trip.Segments));
        Assert.Empty(dbContext.ChangeTracker.Entries<CityActiveTrip>());
    }

    [Fact]
    public async Task ListActiveForUpdateByCityId_ReturnsTrackedTripsSortedAscending()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ListActiveForUpdateByCityId_ReturnsTrackedTripsSortedAscending));
        City city = RepositoryTestData.CreateCity();
        District fromDistrict = RepositoryTestData.CreateDistrict(city.Id, "From");
        District toDistrict = RepositoryTestData.CreateDistrict(city.Id, "To", createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1));
        RoadNode fromNode = RepositoryTestData.CreateRoadNode(city.Id, fromDistrict.Id, "From Node");
        RoadNode midNode = RepositoryTestData.CreateRoadNode(city.Id, fromDistrict.Id, "Mid Node", positionX: 20m);
        RoadNode toNode = RepositoryTestData.CreateRoadNode(city.Id, toDistrict.Id, "To Node", positionX: 30m);
        RoadSegment firstSegment = RepositoryTestData.CreateRoadSegment(city.Id, fromDistrict.Id, fromNode.Id, midNode.Id, "First");
        RoadSegment secondSegment = RepositoryTestData.CreateRoadSegment(city.Id, toDistrict.Id, midNode.Id, toNode.Id, "Second");

        CityActiveTrip older = RepositoryTestData.CreateTrip(
            city.Id,
            fromDistrict.Id,
            toDistrict.Id,
            fromNode.Id,
            midNode.Id,
            toNode.Id,
            firstSegment.Id,
            secondSegment.Id,
            startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(3));
        CityActiveTrip newer = RepositoryTestData.CreateTrip(
            city.Id,
            fromDistrict.Id,
            toDistrict.Id,
            fromNode.Id,
            midNode.Id,
            toNode.Id,
            firstSegment.Id,
            secondSegment.Id,
            startedAtSimTimeUtc: RepositoryTestData.BaseUtc.AddHours(4));

        await dbContext.Cities.AddAsync(city);
        await dbContext.Districts.AddRangeAsync(fromDistrict, toDistrict);
        await dbContext.RoadNodes.AddRangeAsync(fromNode, midNode, toNode);
        await dbContext.RoadSegments.AddRangeAsync(firstSegment, secondSegment);
        await dbContext.Set<CityActiveTrip>().AddRangeAsync(older, newer);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new CityActiveTripRepository(dbContext);

        IReadOnlyList<CityActiveTrip> result = await repository.ListActiveForUpdateByCityIdAsync(city.Id, CancellationToken.None);
        result[0].AdvanceTo(result[0].StartedAtSimTimeUtc.AddMinutes(2), 99);
        await dbContext.SaveChangesAsync();

        CityActiveTrip persistedOlder = await dbContext.Set<CityActiveTrip>()
           .AsNoTracking()
           .SingleAsync(x => x.Id == older.Id);

        Assert.Equal([older.Id, newer.Id], result.Select(static x => x.Id).ToArray());
        Assert.All(result, static trip => Assert.Single(trip.Segments));
        Assert.Equal(2, dbContext.ChangeTracker.Entries<CityActiveTrip>().Count());
        Assert.Equal(99, persistedOlder.LastAdvancedTickId);
    }

    [Fact]
    public async Task AddAsync_WhenTripIsAdded_PersistsTripAndOwnedSegments()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(AddAsync_WhenTripIsAdded_PersistsTripAndOwnedSegments));
        City city = RepositoryTestData.CreateCity();
        District fromDistrict = RepositoryTestData.CreateDistrict(city.Id, "From");
        District toDistrict = RepositoryTestData.CreateDistrict(city.Id, "To", createdAtUtc: RepositoryTestData.BaseUtc.AddMinutes(1));
        RoadNode fromNode = RepositoryTestData.CreateRoadNode(city.Id, fromDistrict.Id, "From Node");
        RoadNode midNode = RepositoryTestData.CreateRoadNode(city.Id, fromDistrict.Id, "Mid Node", positionX: 20m);
        RoadNode toNode = RepositoryTestData.CreateRoadNode(city.Id, toDistrict.Id, "To Node", positionX: 30m);
        RoadSegment firstSegment = RepositoryTestData.CreateRoadSegment(city.Id, fromDistrict.Id, fromNode.Id, midNode.Id, "First");
        RoadSegment secondSegment = RepositoryTestData.CreateRoadSegment(city.Id, toDistrict.Id, midNode.Id, toNode.Id, "Second");
        CityActiveTrip trip = RepositoryTestData.CreateTrip(
            city.Id,
            fromDistrict.Id,
            toDistrict.Id,
            fromNode.Id,
            midNode.Id,
            toNode.Id,
            firstSegment.Id,
            secondSegment.Id);

        await dbContext.Cities.AddAsync(city);
        await dbContext.Districts.AddRangeAsync(fromDistrict, toDistrict);
        await dbContext.RoadNodes.AddRangeAsync(fromNode, midNode, toNode);
        await dbContext.RoadSegments.AddRangeAsync(firstSegment, secondSegment);
        await dbContext.SaveChangesAsync();
        var repository = new CityActiveTripRepository(dbContext);

        await repository.AddAsync(trip, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        CityActiveTrip persistedTrip = await dbContext.Set<CityActiveTrip>()
           .AsNoTracking()
           .Include(x => x.Segments)
           .SingleAsync(x => x.Id == trip.Id);

        Assert.Equal(trip.CityId, persistedTrip.CityId);
        CityActiveTripSegment segment = Assert.Single(persistedTrip.Segments);
        Assert.Equal(firstSegment.Id, segment.RoadSegmentId);
    }
}
