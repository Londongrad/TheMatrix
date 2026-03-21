using FluentValidation;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock
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
