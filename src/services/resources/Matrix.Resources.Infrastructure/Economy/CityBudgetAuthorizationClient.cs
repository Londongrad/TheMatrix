using System.Net.Http.Json;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;

namespace Matrix.Resources.Infrastructure.Economy
{
    internal sealed class CityBudgetAuthorizationClient(HttpClient client) : ICityBudgetAuthorizationClient
    {
        private readonly HttpClient _client = client;

        public async Task<CityBudgetAuthorizationDecision> AuthorizeAsync(
            CityBudgetAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            string url = $"/api/economy/Budget/cities/{request.CityId}/operation-authorizations";
            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: new AuthorizeBudgetOperationRequest(
                    Category: request.Category,
                    OperationKind: request.OperationKind,
                    RequestedIntensity: request.RequestedIntensity,
                    EstimatedAmount: request.EstimatedAmount,
                    EmergencyOverride: request.EmergencyOverrideRequested),
                cancellationToken: cancellationToken);

            response.EnsureSuccessStatusCode();

            BudgetOperationAuthorizationView? payload =
                await response.Content.ReadFromJsonAsync<BudgetOperationAuthorizationView>(
                    cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("Economy budget authorization response was empty.");

            return new CityBudgetAuthorizationDecision(
                Status: payload.Status,
                RequestedIntensity: payload.RequestedIntensity,
                ApprovedIntensity: payload.ApprovedIntensity,
                AuthorizationLevel: payload.AuthorizationLevel,
                AvailableAmount: payload.AvailableAmount,
                PressureIndex: payload.PressureIndex,
                EmergencyOverrideRequested: payload.EmergencyOverrideRequested,
                AuthorizedByEmergencyOverride: payload.AuthorizedByEmergencyOverride,
                Summary: payload.Summary);
        }
    }
}
