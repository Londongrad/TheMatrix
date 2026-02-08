using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.SetClockSpeed
{
    public sealed class SetClockSpeedCommandHandler(ISimulationClockMutationExecutor mutationExecutor)
        : IRequestHandler<SetClockSpeedCommand, bool>
    {
        public Task<bool> Handle(
            SetClockSpeedCommand request,
            CancellationToken cancellationToken)
        {
            return mutationExecutor.ExecuteAsync(
                simulationId: new SimulationId(request.SimulationId),
                mutate: clock => clock.SetSpeed(SimSpeed.From(request.Multiplier)),
                cancellationToken: cancellationToken);
        }
    }
}
