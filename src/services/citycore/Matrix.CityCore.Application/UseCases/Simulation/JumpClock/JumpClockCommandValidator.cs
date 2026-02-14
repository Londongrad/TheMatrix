using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Simulation.JumpClock
{
    public sealed class JumpClockCommandValidator : AbstractValidator<JumpClockCommand>
    {
        public JumpClockCommandValidator()
        {
            RuleFor(x => x.SimulationId)
               .NotEmpty();

            RuleFor(x => x.NewSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("NewSimTimeUtc must be in UTC (Offset=00:00).");
        }
    }
}
