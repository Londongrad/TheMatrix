using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.DispatchCityWaterDistributionMaintenance
{
    public sealed class DispatchCityWaterDistributionMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard)
        : IRequestHandler<DispatchCityWaterDistributionMaintenanceCommand, CityWaterDistributionStatusDto?>
    {
        public async Task<CityWaterDistributionStatusDto?> Handle(
            DispatchCityWaterDistributionMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            WaterDistributionMaintenanceFocus focus = Enum.Parse<WaterDistributionMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            WaterDistributionMaintenanceIntensity requestedIntensity = Enum.Parse<WaterDistributionMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: requestedIntensity.ToString(),
                budget: state.OperationalBudgetPressure.ToSnapshot(),
                emergencyModeEnabled: state.WaterDistributionInfrastructure.EmergencyModeEnabled);
            WaterDistributionMaintenanceIntensity appliedIntensity = Enum.Parse<WaterDistributionMaintenanceIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.DispatchWaterDistributionMaintenance(
                focus: focus,
                intensity: appliedIntensity);

            var refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateInfrastructureMaintenanceExpense(
                    cityId: request.CityId,
                    systemName: "WaterDistribution",
                    operationKind: "WaterDistributionMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    occurredAtUtc: DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal waterSupport = pressureProfileFactory.Create(state).WaterSupport;

            return CityWaterDistributionStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                waterSupportIndex: waterSupport,
                requestedIntensity: budgetDecision.RequestedIntensity,
                appliedIntensity: budgetDecision.AppliedIntensity);
        }
    }
}
