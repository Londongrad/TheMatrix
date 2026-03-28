using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
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
