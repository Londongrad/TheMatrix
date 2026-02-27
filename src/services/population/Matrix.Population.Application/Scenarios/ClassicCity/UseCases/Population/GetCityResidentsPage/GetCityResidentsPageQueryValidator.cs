using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage
{
    public sealed class GetCityResidentsPageQueryValidator : AbstractValidator<GetCityResidentsPageQuery>
    {
        public GetCityResidentsPageQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.Pagination)
               .NotNull();
        }
    }
}
