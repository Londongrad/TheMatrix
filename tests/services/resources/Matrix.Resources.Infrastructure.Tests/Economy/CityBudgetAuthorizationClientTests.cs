using System.Net;
using System.Net.Http.Json;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Infrastructure.Economy;
using Matrix.Resources.Infrastructure.Tests.Http;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Economy;

public sealed class CityBudgetAuthorizationClientTests
{
    [Fact]
    public async Task AuthorizeAsync_PostsExpectedRequestAndMapsResponse()
    {
        var handler = new FakeHttpMessageHandler((request, cancellationToken) =>
        {
            var response = HttpClientTestSupport.CreateJsonResponse(
                new BudgetOperationAuthorizationView(
                    CityId: CityId,
                    Category: "Operations",
                    OperationKind: "StockpileResupplyDispatch",
                    RequestedIntensity: "High",
                    ApprovedIntensity: "Medium",
                    Status: "Approved",
                    AuthorizationLevel: "Medium",
                    AvailableAmount: 520m,
                    EstimatedAmount: 420m,
                    PressureIndex: 0.46m,
                    EmergencyOverrideRequested: false,
                    AuthorizedByEmergencyOverride: false,
                    Summary: "Dispatch approved with reduced intensity."));
            return Task.FromResult(response);
        });
        var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

        CityBudgetAuthorizationDecision result = await client.AuthorizeAsync(
            new CityBudgetAuthorizationRequest(
                CityId: CityId,
                Category: "Operations",
                OperationKind: "StockpileResupplyDispatch",
                RequestedIntensity: "High",
                EstimatedAmount: 420m,
                EmergencyOverrideRequested: false),
            CancellationToken.None);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"/api/economy/Budget/cities/{CityId}/operation-authorizations", request.RequestUri!.PathAndQuery);

        AuthorizeBudgetOperationRequest? payload = await request.Content!.ReadFromJsonAsync<AuthorizeBudgetOperationRequest>();
        Assert.NotNull(payload);
        Assert.Equal("Operations", payload!.Category);
        Assert.Equal("High", payload.RequestedIntensity);
        Assert.Equal(420m, payload.EstimatedAmount);

        Assert.Equal("Approved", result.Status);
        Assert.Equal("Medium", result.ApprovedIntensity);
        Assert.Equal(0.46m, result.PressureIndex);
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsWhenPayloadIsEmpty()
    {
        var handler = new FakeHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            }));
        var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => client.AuthorizeAsync(
            new CityBudgetAuthorizationRequest(CityId, "Operations", "StockpileResupplyDispatch", "High", 420m, false),
            CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsForNonSuccessStatusCode()
    {
        var handler = new FakeHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.AuthorizeAsync(
            new CityBudgetAuthorizationRequest(CityId, "Operations", "StockpileResupplyDispatch", "High", 420m, false),
            CancellationToken.None));
    }
}
