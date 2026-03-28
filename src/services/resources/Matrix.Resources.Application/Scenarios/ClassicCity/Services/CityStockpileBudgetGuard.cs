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
            bool emergencyRationingEnabled)
        {
            int requestedLevel = MapIntensityToLevel(requestedIntensity);
            int maxLevel = 3;

            if (budget.PressureIndex >= 0.55m)
                maxLevel = 2;

            if (budget.PressureIndex >= 0.75m)
                maxLevel = 1;

            if (focus == ResupplyFocus.All && budget.PressureIndex >= 0.45m)
                maxLevel--;

            if (emergencyRationingEnabled)
                maxLevel++;

            maxLevel = Math.Clamp(maxLevel, 1, 3);

            bool blocked = budget.PressureIndex >= 0.90m &&
                           budget.Balance < 0m &&
                           focus == ResupplyFocus.All &&
                           requestedLevel > 1 &&
                           !emergencyRationingEnabled;
            int effectiveLevel = blocked
                ? 1
                : Math.Min(requestedLevel, maxLevel);

            return new CityStockpileBudgetDecision(
                Blocked: blocked,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: MapLevelToIntensity(effectiveLevel),
                PressureIndex: budget.PressureIndex);
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
    }
}
