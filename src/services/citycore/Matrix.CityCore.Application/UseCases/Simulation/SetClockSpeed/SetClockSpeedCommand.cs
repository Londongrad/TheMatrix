using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.SetClockSpeed
{
    public sealed record SetClockSpeedCommand(
        Guid CityId,
        decimal Multiplier) : IRequest<bool>;
}
