namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed class CityMaintenanceBudgetGuard
    {
        public CityMaintenanceBudgetDecision Resolve(
            string requestedIntensity,
            string authorizationLevel,
            decimal pressureIndex,
            bool emergencyModeEnabled)
        {
            string normalizedRequestedIntensity = NormalizeIntensityName(requestedIntensity);
            int requestedLevel = MapIntensityToLevel(normalizedRequestedIntensity);
            int maxLevel = Math.Max(
                val1: 1,
                val2: MapAuthorizationLevelToIntensityLevel(authorizationLevel));

            if (emergencyModeEnabled)
                maxLevel++;

            maxLevel = Math.Clamp(
                value: maxLevel,
                min: 1,
                max: 3);

            return new CityMaintenanceBudgetDecision(
                RequestedIntensity: MapLevelToIntensityName(
                    level: requestedLevel,
                    requestedIntensity: normalizedRequestedIntensity),
                AppliedIntensity: MapLevelToIntensityName(
                    level: Math.Min(
                        val1: requestedLevel,
                        val2: maxLevel),
                    requestedIntensity: normalizedRequestedIntensity),
                PressureIndex: pressureIndex);
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
                ( <= 1, true) => "Low",
                (2, true) => "Medium",
                (_, true) => "High",
                ( <= 1, false) => "Light",
                (2, false) => "Standard",
                _ => "Heavy"
            };
        }

        private static string NormalizeIntensityName(string intensity)
        {
            return (intensity ?? string.Empty).Trim()
               .ToLowerInvariant();
        }

        private static int MapAuthorizationLevelToIntensityLevel(string authorizationLevel)
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
    }
}
