using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.ResumeClock
{
    public sealed record ResumeClockCommand(Guid SimulationId) : IRequest<bool>;
}
