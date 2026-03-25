using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance
{
    public sealed class DispatchCitySanitationMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<DispatchCitySanitationMaintenanceCommand, CitySanitationStatusDto?>
    {
        public async Task<CitySanitationStatusDto?> Handle(
            DispatchCitySanitationMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            SanitationMaintenanceFocus focus = Enum.Parse<SanitationMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            SanitationMaintenanceIntensity intensity = Enum.Parse<SanitationMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);

            state.DispatchSanitationMaintenance(
                focus: focus,
                intensity: intensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal sanitationSupport = pressureProfileFactory.Create(state).SanitationSupport;

            return CitySanitationStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                sanitationSupportIndex: sanitationSupport);
        }
    }
}
