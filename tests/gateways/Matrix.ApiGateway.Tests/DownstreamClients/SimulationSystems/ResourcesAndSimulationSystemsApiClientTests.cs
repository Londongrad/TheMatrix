using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.SimulationSystems;

public sealed class ResourcesAndSimulationSystemsApiClientTests
{
    [Fact]
    public async Task StockpilesApiClientGetCityStockpilesAsync_WhenNotFound_ReturnsNull()
    {
        Guid cityId = Guid.Parse("aa520a7f-4680-4146-bf63-3f64689f262c");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NotFound))
        };
        IStockpilesApiClient client = CreateStockpilesApiClient(CreateHttpClient(handler));

        CityStockpilesView? result = await client.GetCityStockpilesAsync(cityId, CancellationToken.None);

        Assert.Null(result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith($"/api/classic-city/cities/{cityId}/stockpiles", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StockpilesApiClientDispatchCityResupplyAsync_WhenResponseIsSuccessful_ReturnsView()
    {
        Guid cityId = Guid.Parse("ab52f633-47b2-4e85-8c1e-092267335c26");
        DispatchCityResupplyView dispatch = CreateDispatchCityResupplyView(cityId, requestedIntensity: "High", appliedIntensity: "High");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, dispatch))
        };
        IStockpilesApiClient client = CreateStockpilesApiClient(CreateHttpClient(handler));

        DispatchCityResupplyView result = await client.DispatchCityResupplyAsync(
            cityId: cityId,
            request: new DispatchCityResupplyRequest(
                Focus: ResupplyFocus.Food,
                Intensity: ResupplyIntensity.High,
                EmergencyOverride: true),
            cancellationToken: CancellationToken.None);

        Assert.Equal(dispatch, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/api/classic-city/cities/{cityId}/stockpiles/resupply", request.RequestUri, StringComparison.Ordinal);
        Assert.Contains($"\"focus\":{(int)ResupplyFocus.Food}", request.Body, StringComparison.Ordinal);
        Assert.Contains($"\"intensity\":{(int)ResupplyIntensity.High}", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnvironmentalConditionsApiClientGetCityEnvironmentalConditionsAsync_WhenNotFound_ReturnsNull()
    {
        Guid cityId = Guid.Parse("e95b0f3a-f0d1-4a57-b9a8-84c5f34977e4");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NotFound))
        };
        IEnvironmentalConditionsApiClient client = CreateEnvironmentalConditionsApiClient(CreateHttpClient(handler));

        CityEnvironmentalConditionsView? result = await client.GetCityEnvironmentalConditionsAsync(cityId, CancellationToken.None);

        Assert.Null(result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith($"/api/classic-city/cities/{cityId}/environmental-conditions", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnvironmentalConditionsApiClientGetCityDistrictHeatingConditionsAsync_WhenResponseIsSuccessful_ReturnsView()
    {
        Guid cityId = Guid.Parse("0c2d91c7-4f7d-46bc-8c1b-8076ed88bc44");
        CityDistrictHeatingConditionsView heating = CreateCityDistrictHeatingConditionsView(cityId);
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, heating))
        };
        IEnvironmentalConditionsApiClient client = CreateEnvironmentalConditionsApiClient(CreateHttpClient(handler));

        CityDistrictHeatingConditionsView? result = await client.GetCityDistrictHeatingConditionsAsync(cityId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(heating.CityId, result!.CityId);
        Assert.Equal(heating.EffectiveTickId, result.EffectiveTickId);
        Assert.Equal(heating.HeatingSupportIndex, result.HeatingSupportIndex);
        Assert.Equal(Assert.Single(heating.Districts), Assert.Single(result.Districts));
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith($"/api/classic-city/cities/{cityId}/heating/districts", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnvironmentalConditionsApiClientDispatchCityUtilityIncidentResponseAsync_WhenConflictOccurs_ThrowsDownstreamServiceException()
    {
        Guid cityId = Guid.Parse("c27352d5-f522-4d55-b42d-a9f56a9f7b3a");
        CityUtilityIncidentStatusView conflict = CreateCityUtilityIncidentStatusView(cityId, statusIntensity: "Critical");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.Conflict, conflict))
        };
        IEnvironmentalConditionsApiClient client = CreateEnvironmentalConditionsApiClient(CreateHttpClient(handler));

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => client.DispatchCityUtilityIncidentResponseAsync(
                cityId: cityId,
                request: new DispatchCityUtilityIncidentResponseRequest(
                    Focus: "CriticalInfrastructure",
                    Intensity: "Critical"),
                cancellationToken: CancellationToken.None));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Contains("Critical", exception.Body, StringComparison.Ordinal);
    }
}
