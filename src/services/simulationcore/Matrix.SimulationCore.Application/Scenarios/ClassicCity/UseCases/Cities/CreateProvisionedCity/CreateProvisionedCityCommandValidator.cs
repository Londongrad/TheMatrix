using FluentValidation;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateProvisionedCity
{
    public sealed class CreateProvisionedCityCommandValidator : AbstractValidator<CreateProvisionedCityCommand>
    {
        public CreateProvisionedCityCommandValidator()
        {
            RuleFor(x => x.City)
               .NotNull()
               .SetValidator(new CreateCityCommandValidator()!);
        }
    }
}
