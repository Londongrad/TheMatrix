using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Integrations.Economy;
using Matrix.Resources.Infrastructure.Tests.Http;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Integrations.Economy
{
    public sealed class CityBudgetAuthorizationClientTests
    {
        [Fact]
        public async Task AuthorizeAsync_PostsExpectedRequestAndMapsResponse()
        {
            var handler = new FakeHttpMessageHandler((
                request,
                cancellationToken) =>
            {
                HttpResponseMessage response = HttpClientTestSupport.CreateJsonResponse(
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
                request: new CityBudgetAuthorizationRequest(
                    CityId: CityId,
                    Category: "Operations",
                    OperationKind: "StockpileResupplyDispatch",
                    RequestedIntensity: "High",
                    EstimatedAmount: 420m,
                    EmergencyOverrideRequested: false),
                cancellationToken: CancellationToken.None);

            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.Equal(
                expected: $"/api/economy/Budget/cities/{CityId}/operation-authorizations",
                actual: request.RequestUri!.PathAndQuery);

            AuthorizeBudgetOperationRequest? payload =
                await request.Content!.ReadFromJsonAsync<AuthorizeBudgetOperationRequest>();
            Assert.NotNull(payload);
            Assert.Equal(
                expected: "Operations",
                actual: payload!.Category);
            Assert.Equal(
                expected: "High",
                actual: payload.RequestedIntensity);
            Assert.Equal(
                expected: 420m,
                actual: payload.EstimatedAmount);

            Assert.Equal(
                expected: "Approved",
                actual: result.Status);
            Assert.Equal(
                expected: "Medium",
                actual: result.ApprovedIntensity);
            Assert.Equal(
                expected: 0.46m,
                actual: result.PressureIndex);
        }

        [Fact]
        public async Task AuthorizeAsync_ThrowsWhenPayloadIsEmpty()
        {
            var handler = new FakeHttpMessageHandler((
                    request,
                    cancellationToken) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(string.Empty)
                    }));
            var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

            await Assert.ThrowsAsync<JsonException>(() => client.AuthorizeAsync(
                request: new CityBudgetAuthorizationRequest(
                    CityId: CityId,
                    Category: "Operations",
                    OperationKind: "StockpileResupplyDispatch",
                    RequestedIntensity: "High",
                    EstimatedAmount: 420m,
                    EmergencyOverrideRequested: false),
                cancellationToken: CancellationToken.None));
        }

        [Fact]
        public async Task AuthorizeAsync_ThrowsForNonSuccessStatusCode()
        {
            var handler = new FakeHttpMessageHandler((
                    request,
                    cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
            var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

            await Assert.ThrowsAsync<HttpRequestException>(() => client.AuthorizeAsync(
                request: new CityBudgetAuthorizationRequest(
                    CityId: CityId,
                    Category: "Operations",
                    OperationKind: "StockpileResupplyDispatch",
                    RequestedIntensity: "High",
                    EstimatedAmount: 420m,
                    EmergencyOverrideRequested: false),
                cancellationToken: CancellationToken.None));
        }
    }
}
