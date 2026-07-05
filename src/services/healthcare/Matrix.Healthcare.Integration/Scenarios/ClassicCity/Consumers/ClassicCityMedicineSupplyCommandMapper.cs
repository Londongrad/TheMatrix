using Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources;

namespace Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;

internal static class ClassicCityMedicineSupplyCommandMapper
{
    internal static SynchronizeCareMedicineSupplyCommand Map(
        ClassicCityStockpileSnapshotV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Medicine);

        return new SynchronizeCareMedicineSupplyCommand(
            SimulationHostId: message.CityId,
            SourceRevision: message.EffectiveTickId,
            StockLevelIndex: message.Medicine.StockLevelIndex,
            ShortageRiskIndex: message.Medicine.ShortageRiskIndex,
            ObservedAtUtc: message.OccurredAtUtc);
    }
}
