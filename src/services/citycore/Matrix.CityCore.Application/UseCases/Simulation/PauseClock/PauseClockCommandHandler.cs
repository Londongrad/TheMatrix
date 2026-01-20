using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Time;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.PauseClock
{
    public sealed class PauseClockCommandHandler(
        ISimulationClockRepository repository,
        IUnitOfWork unitOfWork) : IRequestHandler<PauseClockCommand, bool>
    {
        public async Task<bool> Handle(
            PauseClockCommand request,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (clock is null)
                return false;

            clock.Pause();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
