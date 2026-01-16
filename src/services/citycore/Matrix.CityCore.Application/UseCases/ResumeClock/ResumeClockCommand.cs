using MediatR;

namespace Matrix.CityCore.Application.UseCases.ResumeClock
{
    public sealed record ResumeClockCommand(Guid CityId) : IRequest<bool>;
}
