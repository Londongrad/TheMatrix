using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Time;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.SetClockSpeed
{
    public sealed class SetClockSpeedCommandHandler(
        ISimulationClockRepository repository,
        IUnitOfWork unitOfWork) : IRequestHandler<SetClockSpeedCommand, bool>
    {
        public async Task<bool> Handle(
            SetClockSpeedCommand request,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (clock is null)
                return false;

            clock.SetSpeed(SimSpeed.From(request.Multiplier));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
