using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.Services
{
    public static class CityStockpileIntegrationEventFactory
    {
        public static ClassicCityStockpileSnapshotV1 CreateSnapshot(
            CityStockpileState state,
            DateTimeOffset occurredAtUtc)
        {
            ArgumentNullException.ThrowIfNull(state);

            return new ClassicCityStockpileSnapshotV1(
                CityId: state.SimulationHostId.Value,
                SupplyStressIndex: state.SupplyStressIndex,
                EmergencyRationingEnabled: state.EmergencyRationingEnabled,
                Fuel: MapLine(state.Fuel.ToSnapshot()),
                Food: MapLine(state.Food.ToSnapshot()),
                Medicine: MapLine(state.Medicine.ToSnapshot()),
                SpareParts: MapLine(state.SpareParts.ToSnapshot()),
                Filters: MapLine(state.Filters.ToSnapshot()),
                EmergencyWater: MapLine(state.EmergencyWater.ToSnapshot()),
                EffectiveTickId: state.LastAppliedTickId,
                EffectiveAtUtc: state.LastEvaluatedAtUtc,
                OccurredAtUtc: occurredAtUtc);
        }

        private static ClassicCityStockpileLineSnapshotV1 MapLine(CityStockpileLineSnapshot snapshot)
        {
            return new ClassicCityStockpileLineSnapshotV1(
                Kind: snapshot.Kind.ToString(),
                StockLevelIndex: snapshot.StockLevelIndex,
                DemandPressureIndex: snapshot.DemandPressureIndex,
                ResupplyReadinessIndex: snapshot.ResupplyReadinessIndex,
                ShortageRiskIndex: snapshot.ShortageRiskIndex);
        }
    }
}
