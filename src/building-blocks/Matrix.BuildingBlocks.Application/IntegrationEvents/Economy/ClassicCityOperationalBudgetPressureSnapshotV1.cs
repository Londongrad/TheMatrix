namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Economy
{
    public sealed record ClassicCityOperationalBudgetPressureSnapshotV1(
        Guid CityId,
        decimal Balance,
        decimal TotalCityExpenses,
        decimal MunicipalOperationsExpenses,
        decimal InfrastructureOperationsExpenses,
        decimal EmergencyOperationsExpenses,
        decimal PressureIndex,
        DateTimeOffset EffectiveAtUtc,
        DateTimeOffset OccurredAtUtc);
}
