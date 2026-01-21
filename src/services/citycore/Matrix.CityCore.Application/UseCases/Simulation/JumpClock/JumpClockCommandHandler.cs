using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.JumpClock
{
    public sealed class JumpClockCommandHandler(
        ISimulationClockRepository repository,
        IUnitOfWork unitOfWork) : IRequestHandler<JumpClockCommand, bool>
    {
        public async Task<bool> Handle(
            JumpClockCommand request,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);
            if (clock is null)
                return false;

            clock.JumpTo(SimTime.FromUtc(request.NewSimTimeUtc));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
