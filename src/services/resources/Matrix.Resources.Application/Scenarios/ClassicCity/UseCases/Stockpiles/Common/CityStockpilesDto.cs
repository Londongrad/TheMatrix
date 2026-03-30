using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common
{
    public sealed record CityStockpilesDto(
        Guid CityId,
        long EffectiveTickId,
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled,
        DateTimeOffset LastEvaluatedAtUtc,
        CityStockpileLineDto Fuel,
        CityStockpileLineDto Food,
        CityStockpileLineDto Medicine,
        CityStockpileLineDto SpareParts,
        CityStockpileLineDto Filters,
        CityStockpileLineDto EmergencyWater)
    {
        public static CityStockpilesDto FromDomain(CityStockpileState state)
        {
            return new CityStockpilesDto(
                CityId: state.SimulationHostId.Value,
                EffectiveTickId: state.LastAppliedTickId,
                SupplyStressIndex: state.SupplyStressIndex,
                EmergencyRationingEnabled: state.EmergencyRationingEnabled,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                Fuel: CityStockpileLineDto.FromDomain(state.Fuel.ToSnapshot()),
                Food: CityStockpileLineDto.FromDomain(state.Food.ToSnapshot()),
                Medicine: CityStockpileLineDto.FromDomain(state.Medicine.ToSnapshot()),
                SpareParts: CityStockpileLineDto.FromDomain(state.SpareParts.ToSnapshot()),
                Filters: CityStockpileLineDto.FromDomain(state.Filters.ToSnapshot()),
                EmergencyWater: CityStockpileLineDto.FromDomain(state.EmergencyWater.ToSnapshot()));
        }
    }
}
