using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.DispatchCitySnowRemovalMaintenance
{
    public sealed class DispatchCitySnowRemovalMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<DispatchCitySnowRemovalMaintenanceCommand, CitySnowRemovalStatusDto?>
    {
        public async Task<CitySnowRemovalStatusDto?> Handle(
            DispatchCitySnowRemovalMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            SnowRemovalMaintenanceFocus focus = Enum.Parse<SnowRemovalMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            SnowRemovalMaintenanceIntensity intensity = Enum.Parse<SnowRemovalMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);

            state.DispatchSnowRemovalMaintenance(
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
                    systemName: "SnowRemoval",
                    operationKind: "SnowRemovalMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: request.Intensity,
                    occurredAtUtc: DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal snowRemovalSupport = pressureProfileFactory.Create(state).SnowRemovalSupport;

            return CitySnowRemovalStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                snowRemovalSupportIndex: snowRemovalSupport);
        }
    }
}
