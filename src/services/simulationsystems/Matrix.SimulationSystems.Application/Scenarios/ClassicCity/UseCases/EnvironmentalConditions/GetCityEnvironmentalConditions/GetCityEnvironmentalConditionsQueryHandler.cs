using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    GetCityEnvironmentalConditions
{
    public sealed class GetCityEnvironmentalConditionsQueryHandler(ICityEnvironmentalConditionRepository repository)
        : IRequestHandler<GetCityEnvironmentalConditionsQuery, CityEnvironmentalConditionsDto?>
    {
        public async Task<CityEnvironmentalConditionsDto?> Handle(
            GetCityEnvironmentalConditionsQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            return state is null
                ? null
                : CityEnvironmentalConditionsDto.FromDomain(state);
        }
    }
}
