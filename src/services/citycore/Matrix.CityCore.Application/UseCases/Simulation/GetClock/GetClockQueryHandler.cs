using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.GetClock
{
    public sealed class GetClockQueryHandler(
        ISimulationClockRepository repository,
        ICityRepository cityRepository)
        : IRequestHandler<GetClockQuery, ClockDto?>
    {
        public async Task<ClockDto?> Handle(
            GetClockQuery request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return null;

            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            return clock is null
                ? null
                : ClockDto.FromDomain(
                    clock: clock,
                    forcePaused: !city.IsActive);
        }
    }
}
