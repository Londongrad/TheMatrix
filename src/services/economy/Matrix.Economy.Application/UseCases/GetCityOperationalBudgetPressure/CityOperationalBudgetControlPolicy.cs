namespace Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure
{
    internal static class CityOperationalBudgetControlPolicy
    {
        public static string ResolveAuthorizationLevel(
            decimal availableAmount,
            decimal pressureIndex)
        {
            if (availableAmount <= 0m)
                return "None";

            if (pressureIndex >= 0.90m && availableAmount < 150m)
                return "None";

            if (availableAmount < 250m || pressureIndex >= 0.75m)
                return "Low";

            if (availableAmount < 600m || pressureIndex >= 0.55m)
                return "Medium";

            return "High";
        }

        public static decimal NormalizeAvailableAmount(decimal availableAmount)
        {
            return decimal.Round(
                d: Math.Max(0m, availableAmount),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
