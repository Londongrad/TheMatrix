using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public static class CityMaintenanceOperationalExpenseFactory
    {
        public static ClassicCityOperationalExpenseIncurredV1 CreateInfrastructureMaintenanceExpense(
            Guid cityId,
            string systemName,
            string operationKind,
            string focus,
            string intensity,
            DateTimeOffset occurredAtUtc)
        {
            string formattedSystemName = SplitPascalCase(systemName);
            string formattedFocus = SplitPascalCase(focus);
            string formattedIntensity = SplitPascalCase(intensity);

            return new ClassicCityOperationalExpenseIncurredV1(
                ExpenseId: Guid.NewGuid(),
                CityId: cityId,
                Category: "Infrastructure",
                Amount: ResolveAmount(
                    systemName: systemName,
                    focus: focus,
                    intensity: intensity,
                    emergencyResponse: false),
                Title: $"Dispatch {formattedSystemName.ToLowerInvariant()} maintenance",
                Description:
                $"{formattedSystemName} maintenance dispatched with {formattedIntensity.ToLowerInvariant()} intensity and {formattedFocus.ToLowerInvariant()} focus.",
                SourceService: "SimulationSystems",
                OperationKind: operationKind,
                OccurredAtUtc: occurredAtUtc);
        }

        public static ClassicCityOperationalExpenseIncurredV1 CreateUtilityIncidentResponseExpense(
            Guid cityId,
            string focus,
            string intensity,
            DateTimeOffset occurredAtUtc)
        {
            string formattedFocus = SplitPascalCase(focus);
            string formattedIntensity = SplitPascalCase(intensity);

            return new ClassicCityOperationalExpenseIncurredV1(
                ExpenseId: Guid.NewGuid(),
                CityId: cityId,
                Category: "Operations",
                Amount: ResolveAmount(
                    systemName: "UtilityIncidents",
                    focus: focus,
                    intensity: intensity,
                    emergencyResponse: true),
                Title: "Dispatch utility incident response",
                Description:
                $"Utility incident response dispatched with {formattedIntensity.ToLowerInvariant()} intensity and {formattedFocus.ToLowerInvariant()} focus.",
                SourceService: "SimulationSystems",
                OperationKind: "UtilityIncidentResponseDispatch",
                OccurredAtUtc: occurredAtUtc);
        }

        private static decimal ResolveAmount(
            string systemName,
            string focus,
            string intensity,
            bool emergencyResponse)
        {
            decimal baseCost = intensity.ToLowerInvariant() switch
            {
                "low" => 110m,
                "medium" => 220m,
                "high" => 380m,
                _ => 220m
            };
            decimal systemMultiplier = systemName switch
            {
                "Drainage" => 1.05m,
                "SnowRemoval" => 1.12m,
                "RoadAccess" => 1.10m,
                "Heating" => 1.28m,
                "WaterDistribution" => 1.24m,
                "Sanitation" => 1.18m,
                "PowerDistribution" => 1.32m,
                "UtilityIncidents" => 1.26m,
                _ => 1m
            };
            decimal focusMultiplier = ResolveFocusMultiplier(focus);

            if (emergencyResponse)
                focusMultiplier += 0.12m;

            return decimal.Round(
                d: baseCost * systemMultiplier * focusMultiplier,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveFocusMultiplier(string focus)
        {
            string normalized = focus.ToLowerInvariant();

            if (normalized.Contains("all") || normalized.Contains("network") || normalized.Contains("citywide"))
                return 1.28m;

            if (normalized.Contains("plant") || normalized.Contains("pump") || normalized.Contains("substation"))
                return 1.18m;

            if (normalized.Contains("emergency") || normalized.Contains("critical"))
                return 1.15m;

            return 1m;
        }

        private static string SplitPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new System.Text.StringBuilder(capacity: value.Length + 4);

            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];

                if (index > 0 && char.IsUpper(current) && !char.IsUpper(value[index - 1]))
                    builder.Append(' ');

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}
