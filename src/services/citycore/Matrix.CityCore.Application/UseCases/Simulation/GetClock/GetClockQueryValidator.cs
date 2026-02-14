using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Simulation.GetClock
{
    public sealed class GetClockQueryValidator : AbstractValidator<GetClockQuery>
    {
        public GetClockQueryValidator()
        {
            RuleFor(x => x.SimulationId)
               .NotEmpty();
        }
    }
}
