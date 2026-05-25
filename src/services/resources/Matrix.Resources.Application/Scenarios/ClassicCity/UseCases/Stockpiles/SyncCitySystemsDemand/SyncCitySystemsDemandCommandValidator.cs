using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand
{
    public sealed class SyncCitySystemsDemandCommandValidator : AbstractValidator<SyncCitySystemsDemandCommand>
    {
        public SyncCitySystemsDemandCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
            RuleFor(x => x.FuelDemandPressureIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.SparePartsDemandPressureIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.FiltersDemandPressureIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.EmergencyWaterDemandPressureIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.OverallDemandPressureIndex)
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
