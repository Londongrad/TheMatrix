using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.DispatchCityPowerDistributionMaintenance
{
    public sealed class DispatchCityPowerDistributionMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<DispatchCityPowerDistributionMaintenanceCommand, CityPowerDistributionStatusDto?>
    {
        public async Task<CityPowerDistributionStatusDto?> Handle(
            DispatchCityPowerDistributionMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            PowerDistributionMaintenanceFocus focus = Enum.Parse<PowerDistributionMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            PowerDistributionMaintenanceIntensity intensity = Enum.Parse<PowerDistributionMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);

            state.DispatchPowerDistributionMaintenance(
                focus: focus,
                intensity: intensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal powerSupport = pressureProfileFactory.Create(state).PowerSupport;

            return CityPowerDistributionStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                powerSupportIndex: powerSupport);
        }
    }
}
