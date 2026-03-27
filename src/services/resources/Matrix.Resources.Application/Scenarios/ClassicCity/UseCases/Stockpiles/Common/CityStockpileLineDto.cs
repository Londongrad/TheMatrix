using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common
{
    public sealed record CityStockpileLineDto(
        string Kind,
        decimal StockLevelIndex,
        decimal DemandPressureIndex,
        decimal ResupplyReadinessIndex,
        decimal ShortageRiskIndex)
    {
        public static CityStockpileLineDto FromDomain(CityStockpileLineSnapshot snapshot)
        {
            return new CityStockpileLineDto(
                Kind: snapshot.Kind.ToString(),
                StockLevelIndex: snapshot.StockLevelIndex,
                DemandPressureIndex: snapshot.DemandPressureIndex,
                ResupplyReadinessIndex: snapshot.ResupplyReadinessIndex,
                ShortageRiskIndex: snapshot.ShortageRiskIndex);
        }
    }
}
