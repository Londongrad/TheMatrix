using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.PauseClock
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
