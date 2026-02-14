using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed class AdvanceSimulationCommandValidator : AbstractValidator<AdvanceSimulationCommand>
    {
        public AdvanceSimulationCommandValidator()
        {
            RuleFor(x => x.SimulationId)
               .NotEmpty();

            RuleFor(x => x.RealDelta)
               .Must(x => x > TimeSpan.Zero)
               .WithMessage("RealDelta must be greater than zero.");
        }
    }
}
