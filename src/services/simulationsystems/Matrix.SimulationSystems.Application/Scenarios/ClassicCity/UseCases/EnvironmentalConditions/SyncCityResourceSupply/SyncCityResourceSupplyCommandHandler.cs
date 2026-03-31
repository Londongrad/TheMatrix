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
                    EffectiveTickId: request.EffectiveTickId,
                    EffectiveAtUtc: request.EffectiveAtUtc);
            }

            if (IsIncomingSnapshotStale(
                    effectiveTickId: request.EffectiveTickId,
                    effectiveAtUtc: request.EffectiveAtUtc,
                    currentEffectiveTickId: state.ResourceSupply.EffectiveTickId,
                    currentEffectiveAtUtc: state.ResourceSupply.EffectiveAtUtc))
            {
                return new SyncCityResourceSupplyResult(
                    Status: SyncCityResourceSupplyStatus.Stale,
                    SupplyStressIndex: state.ResourceSupply.SupplyStressIndex,
                    EffectiveTickId: state.ResourceSupply.EffectiveTickId,
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
                effectiveTickId: request.EffectiveTickId,
                effectiveAtUtc: request.EffectiveAtUtc);

            state.ApplyResourceSupply(resourceSnapshot);

            SyncCityResourceSupplyStatus status = SyncCityResourceSupplyStatus.Deferred;

            if (ShouldApplyAtCurrentProgress(
                    effectiveTickId: request.EffectiveTickId,
                    effectiveAtUtc: request.EffectiveAtUtc,
                    lastAppliedTickId: state.LastAppliedTickId,
                    lastEvaluatedAtUtc: state.LastEvaluatedAtUtc))
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
                EffectiveTickId: state.ResourceSupply.EffectiveTickId,
                EffectiveAtUtc: state.ResourceSupply.EffectiveAtUtc);
        }

        private static bool IsIncomingSnapshotStale(
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            long currentEffectiveTickId,
            DateTimeOffset currentEffectiveAtUtc)
        {
            if (effectiveTickId < currentEffectiveTickId)
                return true;

            if (effectiveTickId > currentEffectiveTickId)
                return false;

            return effectiveAtUtc < currentEffectiveAtUtc;
        }

        private static bool ShouldApplyAtCurrentProgress(
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            long lastAppliedTickId,
            DateTimeOffset lastEvaluatedAtUtc)
        {
            if (effectiveTickId < lastAppliedTickId)
                return true;

            if (effectiveTickId > lastAppliedTickId)
                return false;

            return effectiveAtUtc <= lastEvaluatedAtUtc;
        }
    }
}
