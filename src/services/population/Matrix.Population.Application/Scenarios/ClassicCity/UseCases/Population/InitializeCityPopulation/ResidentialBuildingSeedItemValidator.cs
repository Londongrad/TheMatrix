using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class ResidentialBuildingSeedItemValidator : AbstractValidator<ResidentialBuildingSeedItem>
    {
        public ResidentialBuildingSeedItemValidator()
        {
            RuleFor(x => x.ResidentialBuildingId)
               .NotEmpty();

            RuleFor(x => x.DistrictId)
               .NotEmpty();

            RuleFor(x => x.ResidentCapacity)
               .GreaterThan(0);
        }
    }
}
