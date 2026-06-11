using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    SetCityWaterDistributionEmergencyMode
{
    public sealed class SetCityWaterDistributionEmergencyModeCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<SetCityWaterDistributionEmergencyModeCommand, CityWaterDistributionStatusDto?>
    {
        public async Task<CityWaterDistributionStatusDto?> Handle(
            SetCityWaterDistributionEmergencyModeCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            state.SetWaterDistributionEmergencyMode(request.Enabled);

            CityEnvironmentalConditionSnapshot refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal waterSupport = pressureProfileFactory.Create(state)
               .WaterSupport;

            return CityWaterDistributionStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                waterSupportIndex: waterSupport);
        }
    }
}
