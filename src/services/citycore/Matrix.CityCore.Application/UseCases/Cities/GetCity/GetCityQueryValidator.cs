using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Cities.GetCity
{
    public sealed class GetCityQueryValidator : AbstractValidator<GetCityQuery>
    {
        public GetCityQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
