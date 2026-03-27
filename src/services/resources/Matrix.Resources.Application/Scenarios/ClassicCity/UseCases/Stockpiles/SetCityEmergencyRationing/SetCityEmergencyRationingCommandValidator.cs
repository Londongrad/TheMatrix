using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing
{
    public sealed class SetCityEmergencyRationingCommandValidator : AbstractValidator<SetCityEmergencyRationingCommand>
    {
        public SetCityEmergencyRationingCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
