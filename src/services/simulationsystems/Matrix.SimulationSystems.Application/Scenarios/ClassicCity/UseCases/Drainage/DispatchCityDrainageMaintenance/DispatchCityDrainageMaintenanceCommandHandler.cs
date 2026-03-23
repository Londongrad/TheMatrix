using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance
{
    public sealed class DispatchCityDrainageMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<DispatchCityDrainageMaintenanceCommand, CityDrainageStatusDto?>
    {
        public async Task<CityDrainageStatusDto?> Handle(
            DispatchCityDrainageMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            DrainageMaintenanceFocus focus = Enum.Parse<DrainageMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            DrainageMaintenanceIntensity intensity = Enum.Parse<DrainageMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);

            state.DispatchDrainageMaintenance(
                focus: focus,
                intensity: intensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal drainageSupport = pressureProfileFactory.Create(state).DrainageSupport;

            return CityDrainageStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                drainageSupportIndex: drainageSupport);
        }
    }
}
