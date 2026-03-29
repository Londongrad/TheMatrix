namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityOperationalBudgetPressureSnapshot(
        decimal Balance,
        decimal MunicipalOperationsExpenses,
        decimal GeneralAvailableAmount,
        decimal OperationsAvailableAmount,
        decimal InfrastructureAvailableAmount,
        decimal HealthcareAvailableAmount,
        string GeneralAuthorizationLevel,
        string OperationsAuthorizationLevel,
        string InfrastructureAuthorizationLevel,
        string HealthcareAuthorizationLevel,
        decimal PressureIndex,
        DateTimeOffset EffectiveAtUtc)
    {
        public static CityOperationalBudgetPressureSnapshot Neutral(DateTimeOffset effectiveAtUtc)
        {
            return new CityOperationalBudgetPressureSnapshot(
                Balance: 0m,
                MunicipalOperationsExpenses: 0m,
                GeneralAvailableAmount: 1_000_000m,
                OperationsAvailableAmount: 1_000_000m,
                InfrastructureAvailableAmount: 1_000_000m,
                HealthcareAvailableAmount: 1_000_000m,
                GeneralAuthorizationLevel: "High",
                OperationsAuthorizationLevel: "High",
                InfrastructureAuthorizationLevel: "High",
                HealthcareAuthorizationLevel: "High",
                PressureIndex: 0m,
                EffectiveAtUtc: effectiveAtUtc.Offset == TimeSpan.Zero
                    ? effectiveAtUtc
                    : effectiveAtUtc.ToUniversalTime());
        }
    }
}
