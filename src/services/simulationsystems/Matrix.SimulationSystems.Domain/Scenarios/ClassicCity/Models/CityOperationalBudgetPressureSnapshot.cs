namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityOperationalBudgetPressureSnapshot(
        decimal Balance,
        decimal MunicipalOperationsExpenses,
        decimal PressureIndex,
        DateTimeOffset EffectiveAtUtc)
    {
        public static CityOperationalBudgetPressureSnapshot Neutral(DateTimeOffset effectiveAtUtc)
        {
            return new CityOperationalBudgetPressureSnapshot(
                Balance: 0m,
                MunicipalOperationsExpenses: 0m,
                PressureIndex: 0m,
                EffectiveAtUtc: effectiveAtUtc.Offset == TimeSpan.Zero
                    ? effectiveAtUtc
                    : effectiveAtUtc.ToUniversalTime());
        }
    }
}
