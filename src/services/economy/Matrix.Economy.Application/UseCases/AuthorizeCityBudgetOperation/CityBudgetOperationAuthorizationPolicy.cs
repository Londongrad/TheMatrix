using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Application.UseCases.AuthorizeCityBudgetOperation
{
    public static class CityBudgetOperationAuthorizationPolicy
    {
        public static CityBudgetOperationAuthorizationDto Authorize(
            Guid cityId,
            CityBudgetCategory category,
            string operationKind,
            string requestedIntensity,
            decimal estimatedAmount,
            bool emergencyOverrideRequested,
            CityOperationalBudgetPressureDto pressure)
        {
            string normalizedRequestedIntensity = NormalizeIntensityName(requestedIntensity);
            int requestedLevel = MapIntensityToLevel(normalizedRequestedIntensity);
            (string authorizationLevel, decimal availableAmount) = ResolveBudgetEnvelope(
                category: category,
                pressure: pressure);
            int baseLevel = MapAuthorizationLevelToLevel(authorizationLevel);
            int approvedLevel = ApplyAmountAwareCeiling(
                currentLevel: baseLevel,
                availableAmount: availableAmount,
                estimatedAmount: estimatedAmount);
            bool authorizedByEmergencyOverride = false;

            if (emergencyOverrideRequested &&
                pressure.Balance > 0m &&
                pressure.PressureIndex < 0.9500m)
            {
                int emergencyCeiling = Math.Min(
                    val1: 3,
                    val2: Math.Max(
                              val1: 1,
                              val2: approvedLevel) +
                          1);

                if (emergencyCeiling > approvedLevel)
                {
                    approvedLevel = emergencyCeiling;
                    authorizedByEmergencyOverride = true;
                }
            }

            approvedLevel = Math.Clamp(
                value: approvedLevel,
                min: 0,
                max: 3);

            int effectiveApprovedLevel = Math.Min(
                val1: requestedLevel,
                val2: approvedLevel);
            string status = ResolveStatus(
                requestedLevel: requestedLevel,
                approvedLevel: effectiveApprovedLevel,
                authorizedByEmergencyOverride: authorizedByEmergencyOverride);

            return new CityBudgetOperationAuthorizationDto(
                CityId: cityId,
                Category: category.ToString(),
                OperationKind: operationKind,
                RequestedIntensity: MapLevelToIntensityName(
                    level: requestedLevel,
                    requestedIntensity: normalizedRequestedIntensity),
                ApprovedIntensity: effectiveApprovedLevel <= 0
                    ? null
                    : MapLevelToIntensityName(
                        level: effectiveApprovedLevel,
                        requestedIntensity: normalizedRequestedIntensity),
                Status: status,
                AuthorizationLevel: authorizationLevel,
                AvailableAmount: availableAmount,
                EstimatedAmount: decimal.Round(
                    d: Math.Max(
                        val1: 0m,
                        val2: estimatedAmount),
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero),
                PressureIndex: pressure.PressureIndex,
                EmergencyOverrideRequested: emergencyOverrideRequested,
                AuthorizedByEmergencyOverride: authorizedByEmergencyOverride,
                Summary: BuildSummary(
                    category: category,
                    operationKind: operationKind,
                    authorizationLevel: authorizationLevel,
                    availableAmount: availableAmount,
                    estimatedAmount: estimatedAmount,
                    status: status,
                    approvedIntensity: effectiveApprovedLevel <= 0
                        ? null
                        : MapLevelToIntensityName(
                            level: effectiveApprovedLevel,
                            requestedIntensity: normalizedRequestedIntensity)));
        }

        private static string ResolveStatus(
            int requestedLevel,
            int approvedLevel,
            bool authorizedByEmergencyOverride)
        {
            if (approvedLevel <= 0)
                return "Denied";

            if (authorizedByEmergencyOverride)
                return "ApprovedByEmergencyOverride";

            return approvedLevel < requestedLevel
                ? "ApprovedReduced"
                : "Approved";
        }

        private static int ApplyAmountAwareCeiling(
            int currentLevel,
            decimal availableAmount,
            decimal estimatedAmount)
        {
            decimal normalizedAvailableAmount = Math.Max(
                val1: 0m,
                val2: availableAmount);
            decimal normalizedEstimatedAmount = Math.Max(
                val1: 0m,
                val2: estimatedAmount);

            if (normalizedAvailableAmount <= 0m)
                return 0;

            if (normalizedEstimatedAmount <= 0m)
                return currentLevel;

            decimal ratio = normalizedEstimatedAmount / normalizedAvailableAmount;

            if (ratio > 1.2500m)
                return 0;

            if (ratio > 1.0000m)
                return Math.Min(
                    val1: currentLevel,
                    val2: 1);

            if (ratio > 0.7000m)
                return Math.Min(
                    val1: currentLevel,
                    val2: 2);

            return currentLevel;
        }

        private static (string AuthorizationLevel, decimal AvailableAmount) ResolveBudgetEnvelope(
            CityBudgetCategory category,
            CityOperationalBudgetPressureDto pressure)
        {
            return category switch
            {
                CityBudgetCategory.Infrastructure => (
                    pressure.InfrastructureAuthorizationLevel,
                    pressure.InfrastructureAvailableAmount),
                CityBudgetCategory.Healthcare => (
                    pressure.HealthcareAuthorizationLevel,
                    pressure.HealthcareAvailableAmount),
                CityBudgetCategory.General => (
                    pressure.GeneralAuthorizationLevel,
                    pressure.GeneralAvailableAmount),
                _ => (
                    pressure.OperationsAuthorizationLevel,
                    pressure.OperationsAvailableAmount)
            };
        }

        private static string BuildSummary(
            CityBudgetCategory category,
            string operationKind,
            string authorizationLevel,
            decimal availableAmount,
            decimal estimatedAmount,
            string status,
            string? approvedIntensity)
        {
            string formattedCategory = category.ToString();
            string formattedOperationKind = string.IsNullOrWhiteSpace(operationKind)
                ? "budget-sensitive action"
                : operationKind;
            string availableAmountText = decimal.Round(
                    d: Math.Max(
                        val1: 0m,
                        val2: availableAmount),
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero)
               .ToString("0.##");
            string estimatedAmountText = decimal.Round(
                    d: Math.Max(
                        val1: 0m,
                        val2: estimatedAmount),
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero)
               .ToString("0.##");

            return status switch
            {
                "Denied" =>
                    $"{formattedCategory} budget headroom is too thin for {formattedOperationKind}; estimated cost {estimatedAmountText} exceeds what can be approved right now.",
                "ApprovedReduced" =>
                    $"{formattedCategory} budget approved a lower intensity for {formattedOperationKind}; category level is {authorizationLevel} with {availableAmountText} available right now.",
                "ApprovedByEmergencyOverride" =>
                    $"{formattedCategory} budget accepted an emergency override for {formattedOperationKind}; approved intensity is {approvedIntensity ?? "minimum"} with {availableAmountText} available right now.",
                _ =>
                    $"{formattedCategory} budget approved {formattedOperationKind} at the requested intensity; category level is {authorizationLevel} with {availableAmountText} available right now."
            };
        }

        private static int MapAuthorizationLevelToLevel(string authorizationLevel)
        {
            return authorizationLevel.Trim()
                   .ToLowerInvariant() switch
                {
                    "none" => 0,
                    "low" => 1,
                    "medium" => 2,
                    "high" => 3,
                    _ => 3
                };
        }

        private static int MapIntensityToLevel(string intensity)
        {
            return intensity switch
            {
                "low" or "light" => 1,
                "medium" or "standard" => 2,
                "high" or "heavy" => 3,
                _ => 2
            };
        }

        private static string MapLevelToIntensityName(
            int level,
            string requestedIntensity)
        {
            bool useOperationalVocabulary = requestedIntensity is "low" or "medium" or "high";

            return (level, useOperationalVocabulary) switch
            {
                (<= 1, true) => "Low",
                (2, true) => "Medium",
                (_, true) => "High",
                (<= 1, false) => "Light",
                (2, false) => "Standard",
                _ => "Heavy"
            };
        }

        private static string NormalizeIntensityName(string intensity)
        {
            return (intensity ?? string.Empty).Trim()
               .ToLowerInvariant();
        }
    }
}
