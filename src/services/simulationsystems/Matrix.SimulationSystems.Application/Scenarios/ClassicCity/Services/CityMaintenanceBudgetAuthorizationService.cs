using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class CityMaintenanceBudgetAuthorizationService(ICityBudgetAuthorizationClient client)
    {
        private readonly ICityBudgetAuthorizationClient _client = client;

        public async Task<CityBudgetAuthorizationDecision> AuthorizeInfrastructureMaintenanceAsync(
            Guid cityId,
            string operationKind,
            string requestedIntensity,
            decimal estimatedAmount,
            bool emergencyOverrideRequested,
            bool emergencyModeEnabled,
            string defaultAuthorizationLevel,
            decimal defaultAvailableAmount,
            decimal pressureIndex,
            CancellationToken cancellationToken)
        {
            return await AuthorizeAsync(
                cityId: cityId,
                category: "Infrastructure",
                operationKind: operationKind,
                requestedIntensity: requestedIntensity,
                estimatedAmount: estimatedAmount,
                emergencyOverrideRequested: emergencyOverrideRequested,
                emergencyModeEnabled: emergencyModeEnabled,
                defaultAuthorizationLevel: defaultAuthorizationLevel,
                defaultAvailableAmount: defaultAvailableAmount,
                pressureIndex: pressureIndex,
                cancellationToken: cancellationToken);
        }

        public async Task<CityBudgetAuthorizationDecision> AuthorizeUtilityResponseAsync(
            Guid cityId,
            string operationKind,
            string requestedIntensity,
            decimal estimatedAmount,
            bool emergencyOverrideRequested,
            bool emergencyModeEnabled,
            string defaultAuthorizationLevel,
            decimal defaultAvailableAmount,
            decimal pressureIndex,
            CancellationToken cancellationToken)
        {
            return await AuthorizeAsync(
                cityId: cityId,
                category: "Operations",
                operationKind: operationKind,
                requestedIntensity: requestedIntensity,
                estimatedAmount: estimatedAmount,
                emergencyOverrideRequested: emergencyOverrideRequested,
                emergencyModeEnabled: emergencyModeEnabled,
                defaultAuthorizationLevel: defaultAuthorizationLevel,
                defaultAvailableAmount: defaultAvailableAmount,
                pressureIndex: pressureIndex,
                cancellationToken: cancellationToken);
        }

        private async Task<CityBudgetAuthorizationDecision> AuthorizeAsync(
            Guid cityId,
            string category,
            string operationKind,
            string requestedIntensity,
            decimal estimatedAmount,
            bool emergencyOverrideRequested,
            bool emergencyModeEnabled,
            string defaultAuthorizationLevel,
            decimal defaultAvailableAmount,
            decimal pressureIndex,
            CancellationToken cancellationToken)
        {
            if (!RequiresExplicitAuthorization(
                    requestedIntensity: requestedIntensity,
                    emergencyOverrideRequested: emergencyOverrideRequested,
                    emergencyModeEnabled: emergencyModeEnabled))
                return CityBudgetAuthorizationDecision.NotRequired(
                    requestedIntensity: requestedIntensity,
                    pressureIndex: pressureIndex,
                    authorizationLevel: defaultAuthorizationLevel,
                    availableAmount: defaultAvailableAmount);

            return await _client.AuthorizeAsync(
                request: new CityBudgetAuthorizationRequest(
                    CityId: cityId,
                    Category: category,
                    OperationKind: operationKind,
                    RequestedIntensity: requestedIntensity,
                    EstimatedAmount: estimatedAmount,
                    EmergencyOverrideRequested: emergencyOverrideRequested),
                cancellationToken: cancellationToken);
        }

        private static bool RequiresExplicitAuthorization(
            string requestedIntensity,
            bool emergencyOverrideRequested,
            bool emergencyModeEnabled)
        {
            if (emergencyOverrideRequested || emergencyModeEnabled)
                return true;

            return requestedIntensity.Trim().ToLowerInvariant() switch
            {
                "high" => true,
                "heavy" => true,
                _ => false
            };
        }
    }
}
