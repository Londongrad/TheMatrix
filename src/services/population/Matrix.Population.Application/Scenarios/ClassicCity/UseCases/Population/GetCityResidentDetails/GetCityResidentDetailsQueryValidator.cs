using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails
{
    public sealed class GetCityResidentDetailsQueryValidator : AbstractValidator<GetCityResidentDetailsQuery>
    {
        public GetCityResidentDetailsQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.PersonId)
               .NotEmpty();
        }
    }
}
