using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Integrations.Economy;
using Matrix.SimulationSystems.Infrastructure.Tests.Http;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Scenarios.ClassicCity.Integrations.Economy
{
    public sealed class CityBudgetAuthorizationClientTests
    {
        [Fact]
        public async Task AuthorizeAsync_PostsExpectedRequestAndMapsResponse()
        {
            var handler = new FakeHttpMessageHandler((
                request,
                _) =>
            {
                HttpResponseMessage response = HttpClientTestSupport.CreateJsonResponse(
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
                request: new CityBudgetAuthorizationRequest(
                    CityId: CityId,
                    Category: "Infrastructure",
                    OperationKind: "DrainageMaintenanceDispatch",
                    RequestedIntensity: "Heavy",
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
                expected: "Infrastructure",
                actual: payload!.Category);
            Assert.Equal(
                expected: "DrainageMaintenanceDispatch",
                actual: payload.OperationKind);
            Assert.Equal(
                expected: "Heavy",
                actual: payload.RequestedIntensity);
            Assert.Equal(
                expected: 420m,
                actual: payload.EstimatedAmount);

            Assert.Equal(
                expected: "Approved",
                actual: result.Status);
            Assert.Equal(
                expected: "Standard",
                actual: result.ApprovedIntensity);
            Assert.Equal(
                expected: "Guarded",
                actual: result.AuthorizationLevel);
            Assert.Equal(
                expected: 0.43m,
                actual: result.PressureIndex);
        }

        [Fact]
        public async Task AuthorizeAsync_ThrowsWhenPayloadIsEmpty()
        {
            var handler = new FakeHttpMessageHandler((
                    request,
                    _) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(string.Empty)
                    }));
            var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

            await Assert.ThrowsAsync<JsonException>(() => client.AuthorizeAsync(
                request: new CityBudgetAuthorizationRequest(
                    CityId: CityId,
                    Category: "Infrastructure",
                    OperationKind: "DrainageMaintenanceDispatch",
                    RequestedIntensity: "Heavy",
                    EstimatedAmount: 420m,
                    EmergencyOverrideRequested: false),
                cancellationToken: CancellationToken.None));
        }

        [Fact]
        public async Task AuthorizeAsync_ThrowsForNonSuccessStatusCode()
        {
            var handler = new FakeHttpMessageHandler((
                    request,
                    _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
            var client = new CityBudgetAuthorizationClient(HttpClientTestSupport.CreateHttpClient(handler));

            await Assert.ThrowsAsync<HttpRequestException>(() => client.AuthorizeAsync(
                request: new CityBudgetAuthorizationRequest(
                    CityId: CityId,
                    Category: "Infrastructure",
                    OperationKind: "DrainageMaintenanceDispatch",
                    RequestedIntensity: "Heavy",
                    EstimatedAmount: 420m,
                    EmergencyOverrideRequested: false),
                cancellationToken: CancellationToken.None));
        }
    }
}
