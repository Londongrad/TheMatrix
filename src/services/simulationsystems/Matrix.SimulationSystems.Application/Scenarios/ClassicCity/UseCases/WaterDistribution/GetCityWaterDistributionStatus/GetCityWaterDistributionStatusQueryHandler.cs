using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityWaterDistributionStatus
{
    public sealed class GetCityWaterDistributionStatusQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<GetCityWaterDistributionStatusQuery, CityWaterDistributionStatusDto?>
    {
        public async Task<CityWaterDistributionStatusDto?> Handle(
            GetCityWaterDistributionStatusQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            decimal waterSupport = pressureProfileFactory.Create(state)
               .WaterSupport;

            return CityWaterDistributionStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                waterSupportIndex: waterSupport);
        }
    }
}
