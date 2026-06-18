namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy
{
    public sealed record ClassicCityOperationalBudgetPressureSnapshotV1(
        Guid CityId,
        decimal Balance,
        decimal TotalCityExpenses,
        decimal MunicipalOperationsExpenses,
        decimal InfrastructureOperationsExpenses,
        decimal EmergencyOperationsExpenses,
        decimal GeneralAvailableAmount,
        decimal OperationsAvailableAmount,
        decimal InfrastructureAvailableAmount,
        decimal HealthcareAvailableAmount,
        string GeneralAuthorizationLevel,
        string OperationsAuthorizationLevel,
        string InfrastructureAuthorizationLevel,
        string HealthcareAuthorizationLevel,
        decimal PressureIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc,
        DateTimeOffset OccurredAtUtc);
}
