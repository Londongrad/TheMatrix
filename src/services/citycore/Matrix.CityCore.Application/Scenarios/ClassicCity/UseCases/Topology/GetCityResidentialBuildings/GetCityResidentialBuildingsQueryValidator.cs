using FluentValidation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings
{
    public sealed class GetCityResidentialBuildingsQueryValidator : AbstractValidator<GetCityResidentialBuildingsQuery>
    {
        public GetCityResidentialBuildingsQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            When(
                predicate: x => x.DistrictId.HasValue,
                action: () =>
                {
                    RuleFor(x => x.DistrictId!.Value)
                       .NotEmpty();
                });
        }
    }
}
