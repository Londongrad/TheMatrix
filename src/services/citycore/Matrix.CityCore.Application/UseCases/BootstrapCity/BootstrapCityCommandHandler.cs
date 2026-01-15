using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Time;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.BootstrapCity
{
    public sealed class BootstrapCityCommandHandler(
        ISimulationClockRepository repository,
        IUnitOfWork unitOfWork) : IRequestHandler<BootstrapCityCommand>
    {
        public async Task Handle(
            BootstrapCityCommand request,
            CancellationToken cancellationToken)
        {
            var cityId = new CityId(request.CityId);

            SimulationClock? existing = await repository.GetByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (existing is not null)
                return;

            var clock = SimulationClock.Create(
                cityId: cityId,
                startTime: SimTime.FromUtc(request.StartSimTimeUtc),
                speed: SimSpeed.RealTime());

            await repository.AddAsync(
                clock: clock,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
