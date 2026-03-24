using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus
{
    public sealed class GetCityRoadAccessStatusQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<GetCityRoadAccessStatusQuery, CityRoadAccessStatusDto?>
    {
        public async Task<CityRoadAccessStatusDto?> Handle(
            GetCityRoadAccessStatusQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            decimal roadSupport = pressureProfileFactory.Create(state).RoadSupport;

            return CityRoadAccessStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                roadSupportIndex: roadSupport);
        }
    }
}
