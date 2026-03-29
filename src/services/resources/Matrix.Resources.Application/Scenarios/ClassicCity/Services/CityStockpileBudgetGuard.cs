using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.Services
{
    public sealed class CityStockpileBudgetGuard
    {
        public CityStockpileBudgetDecision ResolveResupply(
            ResupplyFocus focus,
            ResupplyIntensity requestedIntensity,
            CityOperationalBudgetPressureSnapshot budget,
            bool emergencyRationingEnabled,
            bool emergencyOverrideRequested)
        {
            int requestedLevel = MapIntensityToLevel(requestedIntensity);
            (string authorizationLevel, decimal availableAmount) = ResolveBudgetEnvelope(
                focus: focus,
                budget: budget);
            int maxLevel = MapAuthorizationLevelToIntensityLevel(authorizationLevel);

            if (focus == ResupplyFocus.All && maxLevel > 0 && budget.PressureIndex >= 0.45m)
                maxLevel--;

            if (emergencyRationingEnabled && maxLevel < requestedLevel)
                maxLevel++;

            if (emergencyOverrideRequested && maxLevel < requestedLevel)
                maxLevel++;

            maxLevel = Math.Clamp(maxLevel, 0, 3);

            bool blocked = maxLevel <= 0;
            int effectiveLevel = blocked
                ? 1
                : Math.Min(requestedLevel, maxLevel);

            return new CityStockpileBudgetDecision(
                Blocked: blocked,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: MapLevelToIntensity(effectiveLevel),
                PressureIndex: budget.PressureIndex,
                AuthorizationLevel: authorizationLevel,
                AvailableAmount: availableAmount);
        }

        private static int MapIntensityToLevel(ResupplyIntensity intensity)
        {
            return intensity switch
            {
                ResupplyIntensity.Low => 1,
                ResupplyIntensity.Medium => 2,
                ResupplyIntensity.High => 3,
                _ => 2
            };
        }

        private static ResupplyIntensity MapLevelToIntensity(int level)
        {
            return level switch
            {
                <= 1 => ResupplyIntensity.Low,
                2 => ResupplyIntensity.Medium,
                _ => ResupplyIntensity.High
            };
        }

        private static int MapAuthorizationLevelToIntensityLevel(string authorizationLevel)
        {
            return authorizationLevel.Trim().ToLowerInvariant() switch
            {
                "none" => 0,
                "low" => 1,
                "medium" => 2,
                "high" => 3,
                _ => 3
            };
        }

        private static (string AuthorizationLevel, decimal AvailableAmount) ResolveBudgetEnvelope(
            ResupplyFocus focus,
            CityOperationalBudgetPressureSnapshot budget)
        {
            return focus switch
            {
                ResupplyFocus.All => (budget.OperationsAuthorizationLevel, budget.OperationsAvailableAmount),
                ResupplyFocus.Fuel => (budget.InfrastructureAuthorizationLevel, budget.InfrastructureAvailableAmount),
                ResupplyFocus.SpareParts => (budget.InfrastructureAuthorizationLevel, budget.InfrastructureAvailableAmount),
                ResupplyFocus.Filters => (budget.InfrastructureAuthorizationLevel, budget.InfrastructureAvailableAmount),
                ResupplyFocus.EmergencyWater => (budget.InfrastructureAuthorizationLevel, budget.InfrastructureAvailableAmount),
                ResupplyFocus.Medicine => (budget.HealthcareAuthorizationLevel, budget.HealthcareAvailableAmount),
                ResupplyFocus.Food => (budget.GeneralAuthorizationLevel, budget.GeneralAvailableAmount),
                _ => (budget.OperationsAuthorizationLevel, budget.OperationsAvailableAmount)
            };
        }
    }
}
