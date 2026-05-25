namespace Matrix.Resources.Application.Scenarios.ClassicCity.Services
{
    public sealed record CityBudgetAuthorizationDecision(
        string Status,
        string RequestedIntensity,
        string? ApprovedIntensity,
        string AuthorizationLevel,
        decimal AvailableAmount,
        decimal PressureIndex,
        bool EmergencyOverrideRequested,
        bool AuthorizedByEmergencyOverride,
        string Summary)
    {
        public bool Denied => Status.Equals(
            value: "Denied",
            comparisonType: StringComparison.OrdinalIgnoreCase);

        public static CityBudgetAuthorizationDecision NotRequired(
            string requestedIntensity,
            decimal pressureIndex,
            string authorizationLevel,
            decimal availableAmount)
        {
            return new CityBudgetAuthorizationDecision(
                Status: "NotRequired",
                RequestedIntensity: requestedIntensity,
                ApprovedIntensity: requestedIntensity,
                AuthorizationLevel: authorizationLevel,
                AvailableAmount: availableAmount,
                PressureIndex: pressureIndex,
                EmergencyOverrideRequested: false,
                AuthorizedByEmergencyOverride: false,
                Summary: "Explicit budget authorization was not required for this resupply dispatch.");
        }
    }
}
