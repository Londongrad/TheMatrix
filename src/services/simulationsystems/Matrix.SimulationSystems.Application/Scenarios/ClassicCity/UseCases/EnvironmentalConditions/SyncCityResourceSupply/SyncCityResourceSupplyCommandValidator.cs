using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityResourceSupply
{
    public sealed class SyncCityResourceSupplyCommandValidator : AbstractValidator<SyncCityResourceSupplyCommand>
    {
        public SyncCityResourceSupplyCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.SupplyStressIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.FuelStockLevelIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.FuelResupplyReadinessIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.FuelShortageRiskIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.SparePartsStockLevelIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.SparePartsResupplyReadinessIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.SparePartsShortageRiskIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.FiltersStockLevelIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.FiltersResupplyReadinessIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.FiltersShortageRiskIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.EmergencyWaterStockLevelIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.EmergencyWaterResupplyReadinessIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);
            RuleFor(x => x.EmergencyWaterShortageRiskIndex)
               .InclusiveBetween(
                    from: 0m,
                    to: 1m);

            RuleFor(x => x.EffectiveAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("EffectiveAtUtc must be specified in UTC.");
            RuleFor(x => x.EffectiveTickId)
               .GreaterThanOrEqualTo(0L);
        }
    }
}
