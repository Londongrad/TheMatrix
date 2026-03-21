using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.JumpClock
{
    public sealed class JumpClockCommandHandler(ISimulationClockMutationExecutor mutationExecutor)
        : IRequestHandler<JumpClockCommand, bool>
    {
        public Task<bool> Handle(
            JumpClockCommand request,
            CancellationToken cancellationToken)
        {
            return mutationExecutor.ExecuteAsync(
                simulationId: new SimulationId(request.SimulationId),
                mutate: clock => clock.JumpTo(SimTime.FromUtc(request.NewSimTimeUtc)),
                cancellationToken: cancellationToken);
        }
    }
}
