using Matrix.BuildingBlocks.Domain;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityResourceStockLineState
    {
        private CityResourceStockLineState(
            CityResourceKind kind,
            decimal stockLevelIndex,
            decimal demandPressureIndex,
            decimal resupplyReadinessIndex,
            decimal shortageRiskIndex)
        {
            Kind = GuardHelper.AgainstInvalidEnum(
                value: kind,
                propertyName: nameof(kind));
            StockLevelIndex = EnsureIndex(
                value: stockLevelIndex,
                propertyName: nameof(stockLevelIndex));
            DemandPressureIndex = EnsureIndex(
                value: demandPressureIndex,
                propertyName: nameof(demandPressureIndex));
            ResupplyReadinessIndex = EnsureIndex(
                value: resupplyReadinessIndex,
                propertyName: nameof(resupplyReadinessIndex));
            ShortageRiskIndex = EnsureIndex(
                value: shortageRiskIndex,
                propertyName: nameof(shortageRiskIndex));
        }

        private CityResourceStockLineState() { }

        public CityResourceKind Kind { get; private set; }
        public decimal StockLevelIndex { get; private set; }
        public decimal DemandPressureIndex { get; private set; }
        public decimal ResupplyReadinessIndex { get; private set; }
        public decimal ShortageRiskIndex { get; private set; }

        public static CityResourceStockLineState Create(CityStockpileLineSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                propertyName: nameof(snapshot));

            return new CityResourceStockLineState(
                kind: snapshot.Kind,
                stockLevelIndex: snapshot.StockLevelIndex,
                demandPressureIndex: snapshot.DemandPressureIndex,
                resupplyReadinessIndex: snapshot.ResupplyReadinessIndex,
                shortageRiskIndex: snapshot.ShortageRiskIndex);
        }

        public void ApplySnapshot(CityStockpileLineSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                propertyName: nameof(snapshot));

            Kind = GuardHelper.AgainstInvalidEnum(
                value: snapshot.Kind,
                propertyName: nameof(snapshot.Kind));
            StockLevelIndex = EnsureIndex(
                value: snapshot.StockLevelIndex,
                propertyName: nameof(snapshot.StockLevelIndex));
            DemandPressureIndex = EnsureIndex(
                value: snapshot.DemandPressureIndex,
                propertyName: nameof(snapshot.DemandPressureIndex));
            ResupplyReadinessIndex = EnsureIndex(
                value: snapshot.ResupplyReadinessIndex,
                propertyName: nameof(snapshot.ResupplyReadinessIndex));
            ShortageRiskIndex = EnsureIndex(
                value: snapshot.ShortageRiskIndex,
                propertyName: nameof(snapshot.ShortageRiskIndex));
        }

        public CityStockpileLineSnapshot ToSnapshot()
        {
            return new CityStockpileLineSnapshot(
                Kind: Kind,
                StockLevelIndex: StockLevelIndex,
                DemandPressureIndex: DemandPressureIndex,
                ResupplyReadinessIndex: ResupplyReadinessIndex,
                ShortageRiskIndex: ShortageRiskIndex);
        }

        private static decimal EnsureIndex(
            decimal value,
            string propertyName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0m,
                    max: 1m,
                    propertyName: propertyName),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
