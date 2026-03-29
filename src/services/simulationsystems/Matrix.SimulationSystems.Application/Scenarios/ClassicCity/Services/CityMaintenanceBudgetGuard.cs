using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class CityMaintenanceBudgetGuard
    {
        public CityMaintenanceBudgetDecision Resolve(
            string requestedIntensity,
            CityOperationalBudgetPressureSnapshot budget,
            bool emergencyModeEnabled)
        {
            string normalizedRequestedIntensity = NormalizeIntensityName(requestedIntensity);
            int requestedLevel = MapIntensityToLevel(normalizedRequestedIntensity);
            int maxLevel = 3;

            if (budget.PressureIndex >= 0.55m)
                maxLevel = 2;

            if (budget.PressureIndex >= 0.75m)
                maxLevel = 1;

            if (emergencyModeEnabled)
                maxLevel++;

            maxLevel = Math.Clamp(maxLevel, 1, 3);

            return new CityMaintenanceBudgetDecision(
                RequestedIntensity: MapLevelToIntensityName(
                    level: requestedLevel,
                    requestedIntensity: normalizedRequestedIntensity),
                AppliedIntensity: MapLevelToIntensityName(
                    level: Math.Min(requestedLevel, maxLevel),
                    requestedIntensity: normalizedRequestedIntensity),
                PressureIndex: budget.PressureIndex);
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
            return (intensity ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
