using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
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
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc) : IRequest<SyncCityOperationalBudgetPressureResult>;
}
