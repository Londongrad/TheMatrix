using FluentValidation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity
{
    public sealed class DeleteCityCommandValidator : AbstractValidator<DeleteCityCommand>
    {
        public DeleteCityCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
