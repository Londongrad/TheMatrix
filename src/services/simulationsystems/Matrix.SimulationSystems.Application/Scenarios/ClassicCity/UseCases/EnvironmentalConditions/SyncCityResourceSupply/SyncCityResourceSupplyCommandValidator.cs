using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply
{
    public sealed class SyncCityResourceSupplyCommandValidator : AbstractValidator<SyncCityResourceSupplyCommand>
    {
        public SyncCityResourceSupplyCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.SupplyStressIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.FuelStockLevelIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.FuelResupplyReadinessIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.FuelShortageRiskIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.SparePartsStockLevelIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.SparePartsResupplyReadinessIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.SparePartsShortageRiskIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.FiltersStockLevelIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.FiltersResupplyReadinessIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.FiltersShortageRiskIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.EmergencyWaterStockLevelIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.EmergencyWaterResupplyReadinessIndex)
               .InclusiveBetween(0m, 1m);
            RuleFor(x => x.EmergencyWaterShortageRiskIndex)
               .InclusiveBetween(0m, 1m);

            RuleFor(x => x.EffectiveAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("EffectiveAtUtc must be specified in UTC.");
        }
    }
}
