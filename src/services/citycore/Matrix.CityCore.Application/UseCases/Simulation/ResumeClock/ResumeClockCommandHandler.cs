using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.ResumeClock
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
