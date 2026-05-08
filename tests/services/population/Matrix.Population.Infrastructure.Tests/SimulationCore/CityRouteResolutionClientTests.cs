using System.Net;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Infrastructure.SimulationCore;
using Matrix.Population.Infrastructure.Tests.Http;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.Http.HttpClientTestSupport;

namespace Matrix.Population.Infrastructure.Tests.SimulationCore;

public sealed class CityRouteResolutionClientTests
{
    [Fact]
    public async Task ResolveResidentialToAnchorAsync_WhenRouteIsAccessible_ReturnsComputedContext()
    {
        Guid cityId = Guid.Parse("59af9851-18f6-46ab-bd3c-0f0b4d5ca69c");
        Guid residentialBuildingId = Guid.Parse("1e612b91-c7af-4ee8-a578-8f0ad0f7fbd1");
        Guid cityAnchorId = Guid.Parse("f6c6ec55-ad67-4441-a38c-1e09bd2c2fcc");
        HttpClient client = CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"/api/cities/{cityId}/routes/resolve", request.RequestUri!.PathAndQuery);
            return Task.FromResult(JsonResponse(
                """
                {
                  "cityId":"59af9851-18f6-46ab-bd3c-0f0b4d5ca69c",
                  "profile":"Pedestrian",
                  "accessible":true,
                  "usedDynamicRoadConditions":true,
                  "effectiveTickId":12,
                  "conditionsLastEvaluatedAtUtc":"2048-05-06T08:00:00+00:00",
                  "from":{"kind":"ResidentialBuilding","entityId":"1e612b91-c7af-4ee8-a578-8f0ad0f7fbd1","districtId":"0108f45c-76b3-4ecf-a415-e0672d29370c","roadNodeId":"ecf5af9e-0cc2-4e10-b387-7cf135f6f048","name":"Home","positionX":1,"positionY":2},
                  "to":{"kind":"CityAnchor","entityId":"f6c6ec55-ad67-4441-a38c-1e09bd2c2fcc","districtId":"cc9077fb-da71-43be-a042-f2531bcdf6ea","roadNodeId":"9d4f077b-0d52-4915-9a78-5bfa4e569fa3","name":"Office","positionX":3,"positionY":4},
                  "totalDistanceMeters":1200,
                  "estimatedTravelTimeMinutes":24,
                  "overallPassabilityIndex":0.8,
                  "unreachableReason":null,
                  "segments":[]
                }
                """));
        });
        var routeClient = new CityRouteResolutionClient(client);

        CityPopulationCommuteContext? result = await routeClient.ResolveResidentialToAnchorAsync(
            cityId,
            ResidentialBuildingId.From(residentialBuildingId),
            CityAnchorId.From(cityAnchorId),
            profile: "Pedestrian",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.HasRouteData);
        Assert.True(result.IsAccessible);
        Assert.Equal(0.87m, result.AccessibilityIndex);
        Assert.Equal(0.8m, result.PassabilityIndex);
        Assert.Equal(24m, result.EstimatedTravelTimeMinutes);
    }

    [Fact]
    public async Task ResolveResidentialToAnchorAsync_WhenRouteIsBlocked_ReturnsBlockedContext()
    {
        HttpClient client = CreateClient((_, _) =>
            Task.FromResult(JsonResponse(
                """
                {
                  "cityId":"59af9851-18f6-46ab-bd3c-0f0b4d5ca69c",
                  "profile":"Pedestrian",
                  "accessible":false,
                  "usedDynamicRoadConditions":false,
                  "effectiveTickId":null,
                  "conditionsLastEvaluatedAtUtc":null,
                  "from":{"kind":"ResidentialBuilding","entityId":"1e612b91-c7af-4ee8-a578-8f0ad0f7fbd1","districtId":"0108f45c-76b3-4ecf-a415-e0672d29370c","roadNodeId":"ecf5af9e-0cc2-4e10-b387-7cf135f6f048","name":"Home","positionX":1,"positionY":2},
                  "to":{"kind":"CityAnchor","entityId":"f6c6ec55-ad67-4441-a38c-1e09bd2c2fcc","districtId":"cc9077fb-da71-43be-a042-f2531bcdf6ea","roadNodeId":"9d4f077b-0d52-4915-9a78-5bfa4e569fa3","name":"Office","positionX":3,"positionY":4},
                  "totalDistanceMeters":1200,
                  "estimatedTravelTimeMinutes":24,
                  "overallPassabilityIndex":0.2,
                  "unreachableReason":"Closed",
                  "segments":[]
                }
                """)));
        var routeClient = new CityRouteResolutionClient(client);

        CityPopulationCommuteContext? result = await routeClient.ResolveResidentialToAnchorAsync(
            Guid.Parse("59af9851-18f6-46ab-bd3c-0f0b4d5ca69c"),
            ResidentialBuildingId.From(Guid.Parse("1e612b91-c7af-4ee8-a578-8f0ad0f7fbd1")),
            CityAnchorId.From(Guid.Parse("f6c6ec55-ad67-4441-a38c-1e09bd2c2fcc")),
            profile: "Pedestrian",
            cancellationToken: CancellationToken.None);

        Assert.Same(CityPopulationCommuteContext.Blocked, result);
    }

    [Fact]
    public async Task ResolveResidentialToAnchorAsync_WhenResponseIsNonSuccess_ReturnsNull()
    {
        HttpClient client = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)));
        var routeClient = new CityRouteResolutionClient(client);

        CityPopulationCommuteContext? result = await routeClient.ResolveResidentialToAnchorAsync(
            Guid.Parse("59af9851-18f6-46ab-bd3c-0f0b4d5ca69c"),
            ResidentialBuildingId.From(Guid.Parse("1e612b91-c7af-4ee8-a578-8f0ad0f7fbd1")),
            CityAnchorId.From(Guid.Parse("f6c6ec55-ad67-4441-a38c-1e09bd2c2fcc")),
            profile: "Driver",
            cancellationToken: CancellationToken.None);

        Assert.Null(result);
    }
}
