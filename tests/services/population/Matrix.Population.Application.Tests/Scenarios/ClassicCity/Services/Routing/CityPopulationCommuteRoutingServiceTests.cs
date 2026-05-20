using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.Services.Routing;

public sealed class CityPopulationCommuteRoutingServiceTests
{
    private static readonly Guid TestCityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task PreloadAnchorCommutesAsync_WhenRequestsAreEmpty_DoesNotCallBatchClient()
    {
        var client = new RecordingCityRouteResolutionClient();
        var service = new CityPopulationCommuteRoutingService(client);

        await service.PreloadAnchorCommutesAsync(
            cityId: TestCityId,
            requests: [],
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, client.BatchCallCount);
    }

    [Fact]
    public async Task PreloadAnchorCommutesAsync_DeduplicatesIdenticalRequests()
    {
        CityPopulationCommuteRouteRequest request = CreatePreloadRequest();
        CityRouteResolutionBatchRequestItem batchRequest = CreateBatchRequest(request);
        var client = new RecordingCityRouteResolutionClient();
        client.BatchResults[batchRequest] = CreateAccessibleContext(0.75m);
        var service = new CityPopulationCommuteRoutingService(client);

        await service.PreloadAnchorCommutesAsync(
            cityId: TestCityId,
            requests: [request, request, request],
            cancellationToken: CancellationToken.None);

        CityRouteResolutionBatchRequestItem sentRequest = Assert.Single(client.BatchRequests.Single());
        Assert.Equal(1, client.BatchCallCount);
        Assert.Equal(batchRequest, sentRequest);
    }

    [Fact]
    public async Task PreloadAnchorCommutesAsync_SkipsAlreadyCachedRoutes()
    {
        CityPopulationCommuteRouteRequest request = CreatePreloadRequest();
        var client = new RecordingCityRouteResolutionClient
        {
            SingleResult = CreateAccessibleContext(0.65m)
        };
        var service = new CityPopulationCommuteRoutingService(client);

        await service.ResolveAnchorCommuteAsync(
            cityId: TestCityId,
            residentialBuildingId: request.ResidentialBuildingId,
            destinationAnchorId: request.DestinationAnchorId,
            cancellationToken: CancellationToken.None);
        await service.PreloadAnchorCommutesAsync(
            cityId: TestCityId,
            requests: [request],
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, client.SingleCallCount);
        Assert.Equal(0, client.BatchCallCount);
    }

    [Fact]
    public async Task ResolveAnchorCommuteAsync_UsesPreloadedRouteWithoutSingleRouteClientCall()
    {
        CityPopulationCommuteRouteRequest request = CreatePreloadRequest();
        CityPopulationCommuteContext expected = CreateAccessibleContext(0.90m);
        var client = new RecordingCityRouteResolutionClient();
        client.BatchResults[CreateBatchRequest(request)] = expected;
        var service = new CityPopulationCommuteRoutingService(client);

        await service.PreloadAnchorCommutesAsync(
            cityId: TestCityId,
            requests: [request],
            cancellationToken: CancellationToken.None);
        CityPopulationCommuteContext result = await service.ResolveAnchorCommuteAsync(
            cityId: TestCityId,
            residentialBuildingId: request.ResidentialBuildingId,
            destinationAnchorId: request.DestinationAnchorId,
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, client.BatchCallCount);
        Assert.Equal(0, client.SingleCallCount);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task PreloadAnchorCommutesAsync_CachesMissingBatchResultAsNeutral()
    {
        CityPopulationCommuteRouteRequest request = CreatePreloadRequest();
        var client = new RecordingCityRouteResolutionClient
        {
            SingleResult = CreateAccessibleContext(0.80m)
        };
        var service = new CityPopulationCommuteRoutingService(client);

        await service.PreloadAnchorCommutesAsync(
            cityId: TestCityId,
            requests: [request],
            cancellationToken: CancellationToken.None);
        CityPopulationCommuteContext result = await service.ResolveAnchorCommuteAsync(
            cityId: TestCityId,
            residentialBuildingId: request.ResidentialBuildingId,
            destinationAnchorId: request.DestinationAnchorId,
            cancellationToken: CancellationToken.None);

        Assert.Equal(CityPopulationCommuteContext.Neutral, result);
        Assert.Equal(1, client.BatchCallCount);
        Assert.Equal(0, client.SingleCallCount);
    }

    [Fact]
    public async Task PreloadAnchorCommutesAsync_CacheKeyIncludesProfile()
    {
        CityPopulationCommuteRouteRequest pedestrianRequest = CreatePreloadRequest();
        CityPopulationCommuteRouteRequest emergencyRequest = pedestrianRequest with
        {
            Profile = "EmergencyResponse"
        };
        var client = new RecordingCityRouteResolutionClient();
        client.BatchResults[CreateBatchRequest(pedestrianRequest)] = CreateAccessibleContext(0.60m);
        client.BatchResults[CreateBatchRequest(emergencyRequest)] = CreateAccessibleContext(0.95m);
        var service = new CityPopulationCommuteRoutingService(client);

        await service.PreloadAnchorCommutesAsync(
            cityId: TestCityId,
            requests: [pedestrianRequest],
            cancellationToken: CancellationToken.None);
        await service.PreloadAnchorCommutesAsync(
            cityId: TestCityId,
            requests: [emergencyRequest],
            cancellationToken: CancellationToken.None);

        CityRouteResolutionBatchRequestItem sentEmergencyRequest = Assert.Single(client.BatchRequests[1]);
        Assert.Equal(2, client.BatchCallCount);
        Assert.Equal(CreateBatchRequest(emergencyRequest), sentEmergencyRequest);
    }

    private static CityPopulationCommuteRouteRequest CreatePreloadRequest()
    {
        return new CityPopulationCommuteRouteRequest(
            ResidentialBuildingId: ResidentialBuildingId.From(Guid.Parse("10000000-0000-0000-0000-000000000001")),
            DestinationAnchorId: CityAnchorId.From(Guid.Parse("20000000-0000-0000-0000-000000000001")),
            Profile: CityPopulationCommuteRoutingProfiles.Pedestrian);
    }

    private static CityRouteResolutionBatchRequestItem CreateBatchRequest(
        CityPopulationCommuteRouteRequest request)
    {
        return new CityRouteResolutionBatchRequestItem(
            ResidentialBuildingId: request.ResidentialBuildingId,
            CityAnchorId: request.DestinationAnchorId,
            Profile: request.Profile);
    }

    private static CityPopulationCommuteContext CreateAccessibleContext(decimal accessibilityIndex)
    {
        return new CityPopulationCommuteContext(
            HasRouteData: true,
            IsAccessible: true,
            AccessibilityIndex: accessibilityIndex,
            PassabilityIndex: 0.85m,
            EstimatedTravelTimeMinutes: 35m);
    }

    private sealed class RecordingCityRouteResolutionClient : ICityRouteResolutionClient
    {
        public int SingleCallCount { get; private set; }
        public int BatchCallCount { get; private set; }
        public CityPopulationCommuteContext? SingleResult { get; set; }
        public Dictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?> BatchResults { get; } = [];
        public List<IReadOnlyCollection<CityRouteResolutionBatchRequestItem>> BatchRequests { get; } = [];

        public Task<CityPopulationCommuteContext?> ResolveResidentialToAnchorAsync(
            Guid cityId,
            ResidentialBuildingId residentialBuildingId,
            CityAnchorId cityAnchorId,
            string profile,
            CancellationToken cancellationToken)
        {
            SingleCallCount++;
            return Task.FromResult(SingleResult);
        }

        public Task<IReadOnlyDictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?>>
            ResolveResidentialToAnchorsAsync(
                Guid cityId,
                IReadOnlyCollection<CityRouteResolutionBatchRequestItem> requests,
                CancellationToken cancellationToken)
        {
            BatchCallCount++;
            CityRouteResolutionBatchRequestItem[] requestSnapshot = requests.ToArray();
            BatchRequests.Add(requestSnapshot);

            IReadOnlyDictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?> result =
                requestSnapshot
                   .Where(BatchResults.ContainsKey)
                   .ToDictionary(request => request, request => BatchResults[request]);

            return Task.FromResult(result);
        }
    }
}
