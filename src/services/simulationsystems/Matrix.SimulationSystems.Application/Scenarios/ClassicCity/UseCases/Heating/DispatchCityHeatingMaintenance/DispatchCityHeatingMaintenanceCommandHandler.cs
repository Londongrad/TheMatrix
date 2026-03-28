using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.DispatchCityHeatingMaintenance
{
    public sealed class DispatchCityHeatingMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<DispatchCityHeatingMaintenanceCommand, CityHeatingStatusDto?>
    {
        public async Task<CityHeatingStatusDto?> Handle(
            DispatchCityHeatingMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            HeatingMaintenanceFocus focus = Enum.Parse<HeatingMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            HeatingMaintenanceIntensity intensity = Enum.Parse<HeatingMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);

            state.DispatchHeatingMaintenance(
                focus: focus,
                intensity: intensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateInfrastructureMaintenanceExpense(
                    cityId: request.CityId,
                    systemName: "Heating",
                    operationKind: "HeatingMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: request.Intensity,
                    occurredAtUtc: DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal heatingSupport = pressureProfileFactory.Create(state).HeatingSupport;

            return CityHeatingStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                heatingSupportIndex: heatingSupport);
        }
    }
}
