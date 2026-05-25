using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityDistrictPowerDistributionConditions
{
    public sealed class GetCityDistrictPowerDistributionConditionsQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ICityMapTopologyClient cityMapTopologyClient,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        ClassicCityDistrictPowerDistributionProjectionPolicy projectionPolicy)
        : IRequestHandler<GetCityDistrictPowerDistributionConditionsQuery, CityDistrictPowerDistributionConditionsDto?>
    {
        public async Task<CityDistrictPowerDistributionConditionsDto?> Handle(
            GetCityDistrictPowerDistributionConditionsQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            CityRoadGraphTopologyDto? topology = await cityMapTopologyClient.GetRoadGraphAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            if (topology is null)
                return null;

            decimal powerSupportIndex = pressureProfileFactory.Create(state)
               .PowerSupport;
            IReadOnlyList<CityDistrictPowerDistributionConditionDto> districts = projectionPolicy.Project(
                topology: topology,
                state: state,
                powerSupportIndex: powerSupportIndex);

            return new CityDistrictPowerDistributionConditionsDto(
                CityId: request.CityId,
                EffectiveTickId: state.LastAppliedTickId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                PowerSupportIndex: powerSupportIndex,
                Districts: districts);
        }
    }
}
