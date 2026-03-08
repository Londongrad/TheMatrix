using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed class GetCityDashboardQueryValidator : AbstractValidator<GetCityDashboardQuery>
    {
        public GetCityDashboardQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
