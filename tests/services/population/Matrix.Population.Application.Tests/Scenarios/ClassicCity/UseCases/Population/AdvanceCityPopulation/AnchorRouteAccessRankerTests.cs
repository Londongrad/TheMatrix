using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class AnchorRouteAccessRankerTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly ResidentialBuildingId TestResidentialBuildingId = ResidentialBuildingId.From(
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RankAsync_WhenResidentialBuildingIsMissing_ReturnsEmptyAndDoesNotCallRouting()
    {
        CityPopulationAnchorCatalogItem anchor = CreateAnchor(anchorId: CreateAnchorId(1));
        var routingService = new RecordingCommuteRoutingService();

        IReadOnlyList<CityAnchorId> result = await AnchorRouteAccessRanker.RankAsync(
            cityId: TestCityId,
            residentialBuildingId: null,
            anchors: [anchor],
            commuteRoutingService: routingService,
            cancellationToken: CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(0, routingService.PreloadCallCount);
        Assert.Empty(routingService.ResolvedDestinationAnchorIds);
    }

    [Fact]
    public async Task RankAsync_WhenAnchorsAreEmpty_ReturnsEmptyAndDoesNotCallRouting()
    {
        var routingService = new RecordingCommuteRoutingService();

        IReadOnlyList<CityAnchorId> result = await AnchorRouteAccessRanker.RankAsync(
            cityId: TestCityId,
            residentialBuildingId: TestResidentialBuildingId,
            anchors: [],
            commuteRoutingService: routingService,
            cancellationToken: CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(0, routingService.PreloadCallCount);
        Assert.Empty(routingService.ResolvedDestinationAnchorIds);
    }

    [Fact]
    public async Task RankAsync_WhenAnchorsExist_PreloadsPedestrianRoutesAndResolvesEveryAnchor()
    {
        CityAnchorId firstAnchorId = CreateAnchorId(2);
        CityAnchorId secondAnchorId = CreateAnchorId(3);
        CityPopulationAnchorCatalogItem firstAnchor = CreateAnchor(anchorId: firstAnchorId);
        CityPopulationAnchorCatalogItem secondAnchor = CreateAnchor(anchorId: secondAnchorId);
        var routingService = new RecordingCommuteRoutingService();

        IReadOnlyList<CityAnchorId> result = await AnchorRouteAccessRanker.RankAsync(
            cityId: TestCityId,
            residentialBuildingId: TestResidentialBuildingId,
            anchors: [firstAnchor, secondAnchor],
            commuteRoutingService: routingService,
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, routingService.PreloadCallCount);
        Assert.Equal(2, routingService.PreloadRequests.Count);
        Assert.All(
            routingService.PreloadRequests,
            request =>
            {
                Assert.Equal(TestResidentialBuildingId, request.ResidentialBuildingId);
                Assert.Equal(CityPopulationCommuteRoutingProfiles.Pedestrian, request.Profile);
            });
        Assert.Equal(
            [firstAnchorId, secondAnchorId],
            routingService.PreloadRequests.Select(request => request.DestinationAnchorId).ToArray());
        Assert.Equal(
            [firstAnchorId, secondAnchorId],
            routingService.ResolvedDestinationAnchorIds.ToArray());
        Assert.Equal([firstAnchorId, secondAnchorId], result);
    }

    [Fact]
    public async Task RankAsync_SortsAccessibleRoutesBeforeInaccessibleRoutes()
    {
        CityAnchorId inaccessibleAnchorId = CreateAnchorId(4);
        CityAnchorId accessibleAnchorId = CreateAnchorId(5);
        var routingService = new RecordingCommuteRoutingService();
        routingService.SetContext(
            destinationAnchorId: inaccessibleAnchorId,
            context: CreateCommute(
                isAccessible: false,
                accessibilityIndex: 1m,
                passabilityIndex: 1m,
                estimatedTravelTimeMinutes: 1m));
        routingService.SetContext(
            destinationAnchorId: accessibleAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.1m,
                passabilityIndex: 0.1m,
                estimatedTravelTimeMinutes: 90m));

        IReadOnlyList<CityAnchorId> result = await RankAsync([inaccessibleAnchorId, accessibleAnchorId], routingService);

        Assert.Equal([accessibleAnchorId, inaccessibleAnchorId], result);
    }

    [Fact]
    public async Task RankAsync_SortsByHigherAccessibilityIndex()
    {
        CityAnchorId lowerAccessibilityAnchorId = CreateAnchorId(6);
        CityAnchorId higherAccessibilityAnchorId = CreateAnchorId(7);
        var routingService = new RecordingCommuteRoutingService();
        routingService.SetContext(
            destinationAnchorId: lowerAccessibilityAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.5m,
                passabilityIndex: 1m,
                estimatedTravelTimeMinutes: 15m));
        routingService.SetContext(
            destinationAnchorId: higherAccessibilityAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.9m,
                passabilityIndex: 1m,
                estimatedTravelTimeMinutes: 15m));

        IReadOnlyList<CityAnchorId> result = await RankAsync(
            [lowerAccessibilityAnchorId, higherAccessibilityAnchorId],
            routingService);

        Assert.Equal([higherAccessibilityAnchorId, lowerAccessibilityAnchorId], result);
    }

    [Fact]
    public async Task RankAsync_SortsByHigherPassabilityIndexWhenAccessibilityTies()
    {
        CityAnchorId lowerPassabilityAnchorId = CreateAnchorId(8);
        CityAnchorId higherPassabilityAnchorId = CreateAnchorId(9);
        var routingService = new RecordingCommuteRoutingService();
        routingService.SetContext(
            destinationAnchorId: lowerPassabilityAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.8m,
                passabilityIndex: 0.4m,
                estimatedTravelTimeMinutes: 15m));
        routingService.SetContext(
            destinationAnchorId: higherPassabilityAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.8m,
                passabilityIndex: 0.9m,
                estimatedTravelTimeMinutes: 15m));

        IReadOnlyList<CityAnchorId> result = await RankAsync(
            [lowerPassabilityAnchorId, higherPassabilityAnchorId],
            routingService);

        Assert.Equal([higherPassabilityAnchorId, lowerPassabilityAnchorId], result);
    }

    [Fact]
    public async Task RankAsync_SortsByLowerTravelTimeWhenAccessAndPassabilityTie()
    {
        CityAnchorId slowerAnchorId = CreateAnchorId(10);
        CityAnchorId fasterAnchorId = CreateAnchorId(11);
        var routingService = new RecordingCommuteRoutingService();
        routingService.SetContext(
            destinationAnchorId: slowerAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.8m,
                passabilityIndex: 0.9m,
                estimatedTravelTimeMinutes: 30m));
        routingService.SetContext(
            destinationAnchorId: fasterAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.8m,
                passabilityIndex: 0.9m,
                estimatedTravelTimeMinutes: 10m));

        IReadOnlyList<CityAnchorId> result = await RankAsync([slowerAnchorId, fasterAnchorId], routingService);

        Assert.Equal([fasterAnchorId, slowerAnchorId], result);
    }

    [Fact]
    public async Task RankAsync_TreatsNullTravelTimeAsMaxValue()
    {
        CityAnchorId unknownTravelAnchorId = CreateAnchorId(12);
        CityAnchorId knownTravelAnchorId = CreateAnchorId(13);
        var routingService = new RecordingCommuteRoutingService();
        routingService.SetContext(
            destinationAnchorId: unknownTravelAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.8m,
                passabilityIndex: 0.9m,
                estimatedTravelTimeMinutes: null));
        routingService.SetContext(
            destinationAnchorId: knownTravelAnchorId,
            context: CreateCommute(
                isAccessible: true,
                accessibilityIndex: 0.8m,
                passabilityIndex: 0.9m,
                estimatedTravelTimeMinutes: 90m));

        IReadOnlyList<CityAnchorId> result = await RankAsync(
            [unknownTravelAnchorId, knownTravelAnchorId],
            routingService);

        Assert.Equal([knownTravelAnchorId, unknownTravelAnchorId], result);
    }

    private static async Task<IReadOnlyList<CityAnchorId>> RankAsync(
        IReadOnlyList<CityAnchorId> anchorIds,
        RecordingCommuteRoutingService routingService)
    {
        CityPopulationAnchorCatalogItem[] anchors = anchorIds
           .Select(anchorId => CreateAnchor(anchorId: anchorId))
           .ToArray();

        return await AnchorRouteAccessRanker.RankAsync(
            cityId: TestCityId,
            residentialBuildingId: TestResidentialBuildingId,
            anchors: anchors,
            commuteRoutingService: routingService,
            cancellationToken: CancellationToken.None);
    }

    private static CityPopulationAnchorCatalogItem CreateAnchor(CityAnchorId anchorId)
    {
        return CityPopulationAnchorCatalogItem.Create(
            cityId: TestCityId,
            cityAnchorId: anchorId,
            districtId: DistrictId.From(Guid.NewGuid()),
            accessRoadNodeId: RoadNodeId.From(Guid.NewGuid()),
            name: $"Anchor {anchorId.Value}",
            type: CityAnchorType.Workplace,
            capacity: 100,
            positionX: 0m,
            positionY: 0m,
            createdAtUtc: CreatedAtUtc);
    }

    private static CityPopulationCommuteContext CreateCommute(
        bool isAccessible,
        decimal accessibilityIndex,
        decimal passabilityIndex,
        decimal? estimatedTravelTimeMinutes)
    {
        return new CityPopulationCommuteContext(
            HasRouteData: true,
            IsAccessible: isAccessible,
            AccessibilityIndex: accessibilityIndex,
            PassabilityIndex: passabilityIndex,
            EstimatedTravelTimeMinutes: estimatedTravelTimeMinutes);
    }

    private static CityAnchorId CreateAnchorId(int index)
    {
        return CityAnchorId.From(Guid.Parse($"cccccccc-cccc-cccc-cccc-{index:000000000000}"));
    }

    private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
    {
        private readonly Dictionary<CityAnchorId, CityPopulationCommuteContext> _contexts = [];

        public List<CityPopulationCommuteRouteRequest> PreloadRequests { get; } = [];
        public List<CityAnchorId?> ResolvedDestinationAnchorIds { get; } = [];
        public int PreloadCallCount { get; private set; }

        public void SetContext(
            CityAnchorId destinationAnchorId,
            CityPopulationCommuteContext context)
        {
            _contexts[destinationAnchorId] = context;
        }

        public Task PreloadAnchorCommutesAsync(
            Guid cityId,
            IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
            CancellationToken cancellationToken)
        {
            PreloadCallCount++;
            PreloadRequests.AddRange(requests);
            return Task.CompletedTask;
        }

        public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? destinationAnchorId,
            CancellationToken cancellationToken)
        {
            ResolvedDestinationAnchorIds.Add(destinationAnchorId);

            return Task.FromResult(
                destinationAnchorId.HasValue &&
                _contexts.TryGetValue(
                    key: destinationAnchorId.Value,
                    value: out CityPopulationCommuteContext? context)
                    ? context
                    : CityPopulationCommuteContext.Neutral);
        }

        public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? healthcareAnchorId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
