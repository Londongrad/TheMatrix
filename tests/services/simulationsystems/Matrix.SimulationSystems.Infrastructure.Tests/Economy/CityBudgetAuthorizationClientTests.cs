using System.Net;
using System.Net.Http.Json;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Infrastructure.Economy;
using Matrix.SimulationSystems.Infrastructure.Tests.Http;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Economy;

public sealed class CityBudgetAuthorizationClientTests
{
    [Fact]
    public async Task AuthorizeAsync_PostsExpectedRequestAndMapsResponse()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            var response = HttpClientTestSupport.CreateJsonResponse(
                new BudgetOperationAuthorizationView(
                    CityId: CityId,
                    Category: "Infrastructure",
                    OperationKind: "DrainageMaintenanceDispatch",
                    RequestedIntensity: "Heavy",
                    ApprovedIntensity: "Standard",
                    Status: "Approved",
                    AuthorizationLevel: "Guarded",
                    AvailableAmount: 540m,
                    EstimatedAmount: 420m,
                    PressureIndex: 0.43m,
                    EmergencyOverrideRequested: false,
                    AuthorizedByEmergencyOverride: false,
                    Summary: "Dispatch approved with reduced intensity."));
            return Task.FromResult(response);
        });
        var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

        CityBudgetAuthorizationDecision result = await client.AuthorizeAsync(
            new CityBudgetAuthorizationRequest(
                CityId: CityId,
                Category: "Infrastructure",
                OperationKind: "DrainageMaintenanceDispatch",
                RequestedIntensity: "Heavy",
                EstimatedAmount: 420m,
                EmergencyOverrideRequested: false),
            CancellationToken.None);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"/api/economy/Budget/cities/{CityId}/operation-authorizations", request.RequestUri!.PathAndQuery);

        AuthorizeBudgetOperationRequest? payload = await request.Content!.ReadFromJsonAsync<AuthorizeBudgetOperationRequest>();
        Assert.NotNull(payload);
        Assert.Equal("Infrastructure", payload!.Category);
        Assert.Equal("DrainageMaintenanceDispatch", payload.OperationKind);
        Assert.Equal("Heavy", payload.RequestedIntensity);
        Assert.Equal(420m, payload.EstimatedAmount);

        Assert.Equal("Approved", result.Status);
        Assert.Equal("Standard", result.ApprovedIntensity);
        Assert.Equal("Guarded", result.AuthorizationLevel);
        Assert.Equal(0.43m, result.PressureIndex);
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsWhenPayloadIsEmpty()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            }));
        var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => client.AuthorizeAsync(
            new CityBudgetAuthorizationRequest(CityId, "Infrastructure", "DrainageMaintenanceDispatch", "Heavy", 420m, false),
            CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsForNonSuccessStatusCode()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.AuthorizeAsync(
            new CityBudgetAuthorizationRequest(CityId, "Infrastructure", "DrainageMaintenanceDispatch", "Heavy", 420m, false),
            CancellationToken.None));
    }
}
