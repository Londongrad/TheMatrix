using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Simulation.PauseClock
{
    public sealed class PauseClockCommandValidator : AbstractValidator<PauseClockCommand>
    {
        public PauseClockCommandValidator()
        {
            RuleFor(x => x.SimulationId)
               .NotEmpty();
        }
    }
}
