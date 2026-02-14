using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Simulation.ResumeClock
{
    public sealed class ResumeClockCommandValidator : AbstractValidator<ResumeClockCommand>
    {
        public ResumeClockCommandValidator()
        {
            RuleFor(x => x.SimulationId)
               .NotEmpty();
        }
    }
}
