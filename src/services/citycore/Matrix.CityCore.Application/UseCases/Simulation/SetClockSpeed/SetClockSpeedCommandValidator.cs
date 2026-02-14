using FluentValidation;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.UseCases.Simulation.SetClockSpeed
{
    public sealed class SetClockSpeedCommandValidator : AbstractValidator<SetClockSpeedCommand>
    {
        public SetClockSpeedCommandValidator()
        {
            RuleFor(x => x.SimulationId)
               .NotEmpty();

            RuleFor(x => x.Multiplier)
               .InclusiveBetween(
                    from: SimSpeed.Min,
                    to: SimSpeed.Max);
        }
    }
}
