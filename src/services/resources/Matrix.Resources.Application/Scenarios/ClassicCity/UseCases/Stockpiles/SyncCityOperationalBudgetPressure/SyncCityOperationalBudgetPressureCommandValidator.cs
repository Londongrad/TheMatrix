using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureCommandValidator
        : AbstractValidator<SyncCityOperationalBudgetPressureCommand>
    {
        public SyncCityOperationalBudgetPressureCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
            RuleFor(x => x.PressureIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.EffectiveTickId)
               .GreaterThanOrEqualTo(0L);
            RuleFor(x => x.EffectiveAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("EffectiveAtUtc must be specified in UTC.");
        }
    }
}
