using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
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
                cityId: new CityId(request.CityId),
                mutate: clock => clock.Pause(),
                cancellationToken: cancellationToken);
        }
    }
}
