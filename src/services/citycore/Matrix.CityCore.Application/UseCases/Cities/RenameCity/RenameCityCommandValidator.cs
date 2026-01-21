using FluentValidation;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.UseCases.Cities.RenameCity
{
    public sealed class RenameCityCommandValidator : AbstractValidator<RenameCityCommand>
    {
        public RenameCityCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.Name)
               .NotEmpty()
               .MaximumLength(CityName.MaxLength);
        }
    }
}
