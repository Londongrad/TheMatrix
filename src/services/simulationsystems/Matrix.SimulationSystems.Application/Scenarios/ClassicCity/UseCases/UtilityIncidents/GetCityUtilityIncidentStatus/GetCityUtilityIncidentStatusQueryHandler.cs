using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityUtilityIncidentStatus
{
    public sealed class GetCityUtilityIncidentStatusQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<GetCityUtilityIncidentStatusQuery, CityUtilityIncidentStatusDto?>
    {
        public async Task<CityUtilityIncidentStatusDto?> Handle(
            GetCityUtilityIncidentStatusQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            decimal utilityIncidentSupport = pressureProfileFactory.Create(state)
               .UtilityIncidentSupport;

            return CityUtilityIncidentStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                utilityIncidentSupportIndex: utilityIncidentSupport);
        }
    }
}
