using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.PauseClock
{
    public sealed class PauseClockCommandHandler(ISimulationClockMutationExecutor mutationExecutor)
        : IRequestHandler<PauseClockCommand, bool>
    {
        public Task<bool> Handle(
            PauseClockCommand request,
            CancellationToken cancellationToken)
        {
            return mutationExecutor.ExecuteAsync(
                simulationId: new SimulationId(request.SimulationId),
                mutate: clock => clock.Pause(),
                cancellationToken: cancellationToken);
        }
    }
}
