using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure
{
    public sealed record SyncCityOperationalBudgetPressureCommand(
        Guid CityId,
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
        DateTimeOffset EffectiveAtUtc) : IRequest<SyncCityOperationalBudgetPressureResult>;
}
