using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
{
    public sealed record SyncCityOperationalBudgetPressureCommand(
        Guid CityId,
        decimal Balance,
        decimal MunicipalOperationsExpenses,
        decimal PressureIndex,
        DateTimeOffset EffectiveAtUtc) : IRequest<SyncCityOperationalBudgetPressureResult>;
}
