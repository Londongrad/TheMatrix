using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureCommandValidator
        : AbstractValidator<SyncCityOperationalBudgetPressureCommand>
    {
        public SyncCityOperationalBudgetPressureCommandValidator()
        {
            RuleFor(x => x.CityId).NotEmpty();
            RuleFor(x => x.PressureIndex).InclusiveBetween(0m, 1m);
        }
    }
}
