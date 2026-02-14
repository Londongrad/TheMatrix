using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed class AdvanceRunningSimulationsCommandValidator : AbstractValidator<AdvanceRunningSimulationsCommand>
    {
        public AdvanceRunningSimulationsCommandValidator()
        {
            RuleFor(x => x.RealDelta)
               .Must(x => x > TimeSpan.Zero)
               .WithMessage("RealDelta must be greater than zero.");
        }
    }
}
