using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    DispatchCityWaterDistributionMaintenance
{
    public sealed class DispatchCityWaterDistributionMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard,
        CityMaintenanceBudgetAuthorizationService budgetAuthorizationService,
        TimeProvider timeProvider)
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
            WaterDistributionMaintenanceIntensity requestedIntensity =
                Enum.Parse<WaterDistributionMaintenanceIntensity>(
                    value: request.Intensity,
                    ignoreCase: true);
            CityBudgetAuthorizationDecision authorizationDecision =
                await budgetAuthorizationService.AuthorizeInfrastructureMaintenanceAsync(
                    cityId: request.CityId,
                    operationKind: "WaterDistributionMaintenanceDispatch",
                    requestedIntensity: request.Intensity,
                    estimatedAmount: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                        systemName: "WaterDistribution",
                        focus: request.Focus,
                        intensity: request.Intensity),
                    emergencyOverrideRequested: request.EmergencyOverride,
                    emergencyModeEnabled: state.WaterDistributionInfrastructure.EmergencyModeEnabled,
                    defaultAuthorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                    defaultAvailableAmount: state.OperationalBudgetPressure.InfrastructureAvailableAmount,
                    pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                    cancellationToken: cancellationToken);

            if (authorizationDecision.Denied)
            {
                decimal deniedSupport = pressureProfileFactory.Create(state)
                   .WaterSupport;

                return CityWaterDistributionStatusDto.FromState(
                    cityId: request.CityId,
                    state: state,
                    waterSupportIndex: deniedSupport,
                    requestedIntensity: request.Intensity,
                    appliedIntensity: null,
                    budgetAuthorizationStatus: authorizationDecision.Status,
                    budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                    budgetAvailableAmount: authorizationDecision.AvailableAmount,
                    budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                    budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                    budgetAuthorizationSummary: authorizationDecision.Summary);
            }

            WaterDistributionMaintenanceIntensity budgetAuthorizedIntensity =
                Enum.Parse<WaterDistributionMaintenanceIntensity>(
                    value: authorizationDecision.ApprovedIntensity ?? requestedIntensity.ToString(),
                    ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: budgetAuthorizedIntensity.ToString(),
                authorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                emergencyModeEnabled: state.WaterDistributionInfrastructure.EmergencyModeEnabled ||
                                      request.EmergencyOverride);
            WaterDistributionMaintenanceIntensity appliedIntensity = Enum.Parse<WaterDistributionMaintenanceIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.ScheduleWaterDistributionMaintenance(
                focus: focus,
                intensity: appliedIntensity,
                readyAtTickId: CalculateReadyAtTickId(
                    currentTickId: state.LastAppliedTickId,
                    intensity: budgetDecision.AppliedIntensity));
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateInfrastructureMaintenanceExpense(
                    cityId: request.CityId,
                    systemName: "WaterDistribution",
                    operationKind: "WaterDistributionMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal waterSupport = pressureProfileFactory.Create(state)
               .WaterSupport;

            return CityWaterDistributionStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                waterSupportIndex: waterSupport,
                requestedIntensity: request.Intensity,
                appliedIntensity: appliedIntensity.ToString(),
                budgetAuthorizationStatus: authorizationDecision.Status,
                budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                budgetAvailableAmount: authorizationDecision.AvailableAmount,
                budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                budgetAuthorizationSummary: authorizationDecision.Summary);
        }

        private static long CalculateReadyAtTickId(
            long currentTickId,
            string intensity)
        {
            long delay = string.Equals(
                a: intensity,
                b: "Heavy",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 2
                : 1;

            return Math.Max(
                val1: 0,
                val2: currentTickId + delay);
        }
    }
}
