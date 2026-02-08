using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.SetClockSpeed
{
    public sealed record SetClockSpeedCommand(
        Guid SimulationId,
        decimal Multiplier) : IRequest<bool>;
}
