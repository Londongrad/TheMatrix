using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Time;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.BootstrapCity
{
    public sealed class BootstrapCityCommandHandler(
        ISimulationClockRepository repository,
        IUnitOfWork unitOfWork) : IRequestHandler<BootstrapCityCommand, Guid>
    {
        public async Task<Guid> Handle(
            BootstrapCityCommand request,
            CancellationToken cancellationToken)
        {
            var clock = SimulationClock.Create(
                startTime: SimTime.FromUtc(request.StartSimTimeUtc),
                speed: SimSpeed.RealTime());

            await repository.AddAsync(
                clock: clock,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return clock.Id.Value;
        }
    }
}
