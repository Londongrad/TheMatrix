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
            CityId cityId = new(request.SimulationId);

            City? city = await cityRepository.GetByIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (city is null)
                return null;

            SimulationClock? clock = await repository.GetBySimulationIdAsync(
                simulationId: new SimulationId(request.SimulationId),
                cancellationToken: cancellationToken);

            return clock is null
                ? null
                : ClockDto.FromDomain(
                    clock: clock,
                    simulationKind: city.SimulationKind.ToString(),
                    forcePaused: !city.IsActive);
        }
    }
}
