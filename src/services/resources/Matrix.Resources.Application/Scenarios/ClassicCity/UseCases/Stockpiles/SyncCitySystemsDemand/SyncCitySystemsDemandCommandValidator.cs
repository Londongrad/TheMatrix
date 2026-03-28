using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand
{
    public sealed class SyncCitySystemsDemandCommandValidator : AbstractValidator<SyncCitySystemsDemandCommand>
    {
        public SyncCitySystemsDemandCommandValidator()
        {
            RuleFor(x => x.CityId).NotEmpty();
            RuleFor(x => x.FuelDemandPressureIndex).InclusiveBetween(0m, 1m);
            RuleFor(x => x.SparePartsDemandPressureIndex).InclusiveBetween(0m, 1m);
            RuleFor(x => x.FiltersDemandPressureIndex).InclusiveBetween(0m, 1m);
            RuleFor(x => x.EmergencyWaterDemandPressureIndex).InclusiveBetween(0m, 1m);
            RuleFor(x => x.OverallDemandPressureIndex).InclusiveBetween(0m, 1m);
        }
    }
}
