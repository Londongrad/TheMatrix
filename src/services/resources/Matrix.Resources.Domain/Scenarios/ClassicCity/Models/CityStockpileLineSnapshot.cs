using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityStockpileLineSnapshot(
        CityResourceKind Kind,
        decimal StockLevelIndex,
        decimal DemandPressureIndex,
        decimal ResupplyReadinessIndex,
        decimal ShortageRiskIndex);
}
