using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure
{
    public sealed record SyncCityOperationalBudgetPressureCommand(
        Guid CityId,
        decimal Balance,
        decimal MunicipalOperationsExpenses,
        decimal PressureIndex,
        DateTimeOffset EffectiveAtUtc) : IRequest<SyncCityOperationalBudgetPressureResult>;
}
