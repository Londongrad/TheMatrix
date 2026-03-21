using FluentValidation;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed
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
