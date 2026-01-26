using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.JumpClock
{
    public sealed class JumpClockCommandHandler(ISimulationClockMutationExecutor mutationExecutor)
        : IRequestHandler<JumpClockCommand, bool>
    {
        public Task<bool> Handle(
            JumpClockCommand request,
            CancellationToken cancellationToken)
        {
            return mutationExecutor.ExecuteAsync(
                cityId: new CityId(request.CityId),
                mutate: clock => clock.JumpTo(SimTime.FromUtc(request.NewSimTimeUtc)),
                cancellationToken: cancellationToken);
        }
    }
}
