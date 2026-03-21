using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock
{
    public sealed class ResumeClockCommandHandler(ISimulationClockMutationExecutor mutationExecutor)
        : IRequestHandler<ResumeClockCommand, bool>
    {
        public Task<bool> Handle(
            ResumeClockCommand request,
            CancellationToken cancellationToken)
        {
            return mutationExecutor.ExecuteAsync(
                simulationId: new SimulationId(request.SimulationId),
                mutate: clock => clock.Resume(),
                cancellationToken: cancellationToken);
        }
    }
}
