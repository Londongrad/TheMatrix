using FluentValidation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity
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
