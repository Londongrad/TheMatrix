using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.ResumeClock
{
    public sealed record ResumeClockCommand(Guid CityId) : IRequest<bool>;
}
