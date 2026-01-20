using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Time;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.ResumeClock
{
    public sealed class ResumeClockCommandHandler(
        ISimulationClockRepository repository,
        IUnitOfWork unitOfWork) : IRequestHandler<ResumeClockCommand, bool>
    {
        public async Task<bool> Handle(
            ResumeClockCommand request,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (clock is null)
                return false;

            clock.Resume();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
