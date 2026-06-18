using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public static class CityPopulationLivingConditionsIntegrationEventFactory
    {
        public static ClassicCityLivingConditionsSnapshotV1 CreateSnapshot(
            CityEnvironmentalConditionState state,
            DateTimeOffset occurredAtUtc)
        {
            ArgumentNullException.ThrowIfNull(state);

            return new ClassicCityLivingConditionsSnapshotV1(
                CityId: state.SimulationHostId.Value,
                FloodingIndex: state.FloodingIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value,
                PowerCoverageIndex: state.PowerCoverageIndex.Value,
                UtilityContinuityIndex: state.UtilityContinuityIndex.Value,
                HeatingCoverageIndex: state.HeatingCoverageIndex.Value,
                WaterCoverageIndex: state.WaterCoverageIndex.Value,
                SanitationCoverageIndex: state.SanitationCoverageIndex.Value,
                EffectiveTickId: state.LastAppliedTickId,
                EffectiveAtUtc: state.LastEvaluatedAtUtc,
                OccurredAtUtc: occurredAtUtc);
        }
    }
}
