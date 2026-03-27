using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply
{
    public sealed class SyncCityResourceSupplyCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<SyncCityResourceSupplyCommand, SyncCityResourceSupplyResult>
    {
        public async Task<SyncCityResourceSupplyResult> Handle(
            SyncCityResourceSupplyCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
            {
                return new SyncCityResourceSupplyResult(
                    Status: SyncCityResourceSupplyStatus.NotInitialized,
                    SupplyStressIndex: 0m,
                    EffectiveAtUtc: request.EffectiveAtUtc);
            }

            if (request.EffectiveAtUtc < state.ResourceSupply.EffectiveAtUtc)
            {
                return new SyncCityResourceSupplyResult(
                    Status: SyncCityResourceSupplyStatus.Stale,
                    SupplyStressIndex: state.ResourceSupply.SupplyStressIndex,
                    EffectiveAtUtc: state.ResourceSupply.EffectiveAtUtc);
            }

            var resourceSnapshot = new CityResourceSupplySnapshot(
                supplyStressIndex: request.SupplyStressIndex,
                fuelStockLevelIndex: request.FuelStockLevelIndex,
                fuelResupplyReadinessIndex: request.FuelResupplyReadinessIndex,
                fuelShortageRiskIndex: request.FuelShortageRiskIndex,
                sparePartsStockLevelIndex: request.SparePartsStockLevelIndex,
                sparePartsResupplyReadinessIndex: request.SparePartsResupplyReadinessIndex,
                sparePartsShortageRiskIndex: request.SparePartsShortageRiskIndex,
                filtersStockLevelIndex: request.FiltersStockLevelIndex,
                filtersResupplyReadinessIndex: request.FiltersResupplyReadinessIndex,
                filtersShortageRiskIndex: request.FiltersShortageRiskIndex,
                emergencyWaterStockLevelIndex: request.EmergencyWaterStockLevelIndex,
                emergencyWaterResupplyReadinessIndex: request.EmergencyWaterResupplyReadinessIndex,
                emergencyWaterShortageRiskIndex: request.EmergencyWaterShortageRiskIndex,
                effectiveAtUtc: request.EffectiveAtUtc);

            state.ApplyResourceSupply(resourceSnapshot);

            SyncCityResourceSupplyStatus status = SyncCityResourceSupplyStatus.Deferred;

            if (request.EffectiveAtUtc <= state.LastEvaluatedAtUtc)
            {
                CitySystemPressureProfile pressure = pressureProfileFactory.Create(
                    state: state,
                    asOfUtc: state.LastEvaluatedAtUtc);

                CityEnvironmentalConditionSnapshot snapshot = policy.Recalculate(
                    state: state,
                    pressure: pressure,
                    asOfUtc: state.LastEvaluatedAtUtc);

                state.ApplySnapshot(snapshot);
                status = SyncCityResourceSupplyStatus.Applied;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SyncCityResourceSupplyResult(
                Status: status,
                SupplyStressIndex: state.ResourceSupply.SupplyStressIndex,
                EffectiveAtUtc: state.ResourceSupply.EffectiveAtUtc);
        }
    }
}
