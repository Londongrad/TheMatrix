using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.Services
{
    public static class CityResupplyOperationalExpenseFactory
    {
        public static ClassicCityOperationalExpenseIncurredV1 CreateDispatchExpense(
            Guid cityId,
            ResupplyFocus focus,
            ResupplyIntensity intensity,
            DateTimeOffset occurredAtUtc)
        {
            string focusLabel = FormatFocus(focus);
            string intensityLabel = FormatIntensity(intensity);

            return new ClassicCityOperationalExpenseIncurredV1(
                ExpenseId: Guid.NewGuid(),
                CityId: cityId,
                Category: ResolveBudgetCategory(focus),
                Amount: EstimateDispatchAmount(
                    focus: focus,
                    intensity: intensity),
                Title: focus == ResupplyFocus.All
                    ? "Dispatch citywide stockpile resupply"
                    : $"Dispatch {focusLabel.ToLowerInvariant()} resupply",
                Description: $"{focusLabel} stockpile resupply dispatched with {intensityLabel.ToLowerInvariant()} intensity.",
                SourceService: "Resources",
                OperationKind: "StockpileResupplyDispatch",
                OccurredAtUtc: occurredAtUtc);
        }

        public static decimal EstimateDispatchAmount(
            ResupplyFocus focus,
            ResupplyIntensity intensity)
        {
            decimal baseCost = intensity switch
            {
                ResupplyIntensity.Low => 120m,
                ResupplyIntensity.Medium => 240m,
                ResupplyIntensity.High => 420m,
                _ => 240m
            };
            decimal focusMultiplier = focus switch
            {
                ResupplyFocus.All => 1.85m,
                ResupplyFocus.Fuel => 1.10m,
                ResupplyFocus.Food => 0.95m,
                ResupplyFocus.Medicine => 1.15m,
                ResupplyFocus.SpareParts => 1.22m,
                ResupplyFocus.Filters => 1.14m,
                ResupplyFocus.EmergencyWater => 1.18m,
                _ => 1m
            };

            return decimal.Round(
                d: baseCost * focusMultiplier,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
        }

        public static string ResolveBudgetCategory(ResupplyFocus focus)
        {
            return focus switch
            {
                ResupplyFocus.Medicine => "Healthcare",
                ResupplyFocus.Fuel => "Infrastructure",
                ResupplyFocus.SpareParts => "Infrastructure",
                ResupplyFocus.Filters => "Infrastructure",
                ResupplyFocus.EmergencyWater => "Infrastructure",
                ResupplyFocus.Food => "General",
                ResupplyFocus.All => "Operations",
                _ => "Operations"
            };
        }

        private static string FormatFocus(ResupplyFocus focus)
        {
            return focus switch
            {
                ResupplyFocus.EmergencyWater => "Emergency water",
                ResupplyFocus.SpareParts => "Spare parts",
                _ => SplitPascalCase(focus.ToString())
            };
        }

        private static string FormatIntensity(ResupplyIntensity intensity)
        {
            return intensity switch
            {
                ResupplyIntensity.Low => "Low",
                ResupplyIntensity.Medium => "Medium",
                ResupplyIntensity.High => "High",
                _ => "Medium"
            };
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
