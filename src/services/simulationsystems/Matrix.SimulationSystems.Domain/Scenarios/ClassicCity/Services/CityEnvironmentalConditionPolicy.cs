using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services
{
    /// <summary>
    ///     First-pass environmental condition policy for Classic City.
    ///     Converts daily weather/system pressure into normalized flooding, snow and access outcomes.
    /// </summary>
    public sealed class CityEnvironmentalConditionPolicy
    {
        public CityEnvironmentalConditionSnapshot CreateSeed(DateTimeOffset asOfUtc)
        {
            EnsureUtc(
                value: asOfUtc,
                paramName: nameof(asOfUtc));

            return new CityEnvironmentalConditionSnapshot(
                drainage: new CitySystemSnapshot(
                    kind: CitySystemKind.Drainage,
                    loadIndex: 0.1000m,
                    serviceQualityIndex: 0.8200m,
                    backlogIndex: 0.0800m,
                    failureRiskIndex: 0.0500m),
                snowRemoval: new CitySystemSnapshot(
                    kind: CitySystemKind.SnowRemoval,
                    loadIndex: 0.0800m,
                    serviceQualityIndex: 0.8000m,
                    backlogIndex: 0.0500m,
                    failureRiskIndex: 0.0400m),
                roadAccess: new CitySystemSnapshot(
                    kind: CitySystemKind.RoadAccess,
                    loadIndex: 0.0900m,
                    serviceQualityIndex: 0.8500m,
                    backlogIndex: 0.0600m,
                    failureRiskIndex: 0.0400m),
                floodingIndex: FloodingIndex.From(0.0400m),
                snowAccumulationIndex: SnowAccumulationIndex.From(0.0200m),
                roadAccessibilityIndex: RoadAccessibilityIndex.From(0.9600m),
                evaluatedAtUtc: asOfUtc);
        }

        public CityEnvironmentalConditionSnapshot Recalculate(
            CityEnvironmentalConditionState state,
            CitySystemPressureProfile pressure,
            DateTimeOffset asOfUtc)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(pressure);
            EnsureUtc(
                value: asOfUtc,
                paramName: nameof(asOfUtc));

            CitySystemSnapshot currentDrainage = state.Drainage.ToSnapshot();
            CitySystemSnapshot currentSnowRemoval = state.SnowRemoval.ToSnapshot();
            CitySystemSnapshot currentRoadAccess = state.RoadAccess.ToSnapshot();

            decimal drainageLoad = Smooth(
                current: currentDrainage.LoadIndex,
                target: Clamp(
                    value: (pressure.RainPressure * 0.72m) +
                           (pressure.StormPressure * 0.33m) +
                           (state.FloodingIndex.Value * 0.18m) -
                           (pressure.DrainageSupport * 0.28m)),
                factor: 0.45m);
            decimal drainageService = Smooth(
                current: currentDrainage.ServiceQualityIndex,
                target: Clamp(
                    value: 0.62m +
                           (pressure.DrainageSupport * 0.33m) -
                           (currentDrainage.BacklogIndex * 0.22m) -
                           (pressure.StormPressure * 0.08m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m);
            decimal drainageBacklog = Smooth(
                current: currentDrainage.BacklogIndex,
                target: Clamp(
                    value: currentDrainage.BacklogIndex +
                           (drainageLoad * 0.22m) -
                           (drainageService * 0.18m) -
                           (pressure.ThawRelief * 0.05m)),
                factor: 0.40m);
            decimal drainageFailureRisk = Smooth(
                current: currentDrainage.FailureRiskIndex,
                target: Clamp(
                    value: (drainageLoad * 0.44m) +
                           (drainageBacklog * 0.34m) +
                           ((1m - drainageService) * 0.30m)),
                factor: 0.30m);

            decimal flooding = Smooth(
                current: state.FloodingIndex.Value,
                target: Clamp(
                    value: state.FloodingIndex.Value +
                           (pressure.RainPressure * 0.26m) +
                           (pressure.StormPressure * 0.18m) +
                           (drainageBacklog * 0.14m) -
                           (drainageService * 0.24m) -
                           (pressure.DrainageSupport * 0.08m)),
                factor: 0.42m);

            decimal snowLoad = Smooth(
                current: currentSnowRemoval.LoadIndex,
                target: Clamp(
                    value: (pressure.SnowPressure * 0.80m) +
                           (pressure.FreezePressure * 0.20m) +
                           (state.SnowAccumulationIndex.Value * 0.22m) -
                           (pressure.SnowRemovalSupport * 0.25m)),
                factor: 0.45m);
            decimal snowService = Smooth(
                current: currentSnowRemoval.ServiceQualityIndex,
                target: Clamp(
                    value: 0.58m +
                           (pressure.SnowRemovalSupport * 0.36m) -
                           (currentSnowRemoval.BacklogIndex * 0.20m) -
                           (pressure.FreezePressure * 0.08m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m);
            decimal snowBacklog = Smooth(
                current: currentSnowRemoval.BacklogIndex,
                target: Clamp(
                    value: currentSnowRemoval.BacklogIndex +
                           (snowLoad * 0.24m) -
                           (snowService * 0.16m) -
                           (pressure.ThawRelief * 0.12m)),
                factor: 0.40m);
            decimal snowFailureRisk = Smooth(
                current: currentSnowRemoval.FailureRiskIndex,
                target: Clamp(
                    value: (snowLoad * 0.40m) +
                           (snowBacklog * 0.32m) +
                           ((1m - snowService) * 0.28m)),
                factor: 0.30m);

            decimal snowAccumulation = Smooth(
                current: state.SnowAccumulationIndex.Value,
                target: Clamp(
                    value: state.SnowAccumulationIndex.Value +
                           (pressure.SnowPressure * 0.30m) +
                           (pressure.FreezePressure * 0.07m) +
                           (snowBacklog * 0.10m) -
                           (snowService * 0.22m) -
                           (pressure.ThawRelief * 0.20m)),
                factor: 0.42m);

            decimal roadLoad = Smooth(
                current: currentRoadAccess.LoadIndex,
                target: Clamp(
                    value: (flooding * 0.32m) +
                           (snowAccumulation * 0.38m) +
                           (pressure.FreezePressure * 0.14m) +
                           (pressure.StormPressure * 0.10m) -
                           (pressure.RoadSupport * 0.18m)),
                factor: 0.45m);
            decimal roadService = Smooth(
                current: currentRoadAccess.ServiceQualityIndex,
                target: Clamp(
                    value: 0.60m +
                           (pressure.RoadSupport * 0.34m) -
                           (currentRoadAccess.BacklogIndex * 0.18m) -
                           ((snowAccumulation + flooding) * 0.10m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m);
            decimal roadBacklog = Smooth(
                current: currentRoadAccess.BacklogIndex,
                target: Clamp(
                    value: currentRoadAccess.BacklogIndex +
                           (roadLoad * 0.16m) -
                           (roadService * 0.14m) -
                           (pressure.ThawRelief * 0.04m)),
                factor: 0.40m);
            decimal roadFailureRisk = Smooth(
                current: currentRoadAccess.FailureRiskIndex,
                target: Clamp(
                    value: (roadLoad * 0.38m) +
                           (roadBacklog * 0.30m) +
                           ((1m - roadService) * 0.25m)),
                factor: 0.30m);

            decimal roadAccessibility = Smooth(
                current: state.RoadAccessibilityIndex.Value,
                target: Clamp(
                    value: 1.02m -
                           (flooding * 0.38m) -
                           (snowAccumulation * 0.42m) -
                           (pressure.FreezePressure * 0.12m) -
                           (roadBacklog * 0.16m) +
                           (roadService * 0.10m) +
                           (pressure.ThawRelief * 0.06m),
                    min: 0.15m,
                    max: 1m),
                factor: 0.50m);

            return new CityEnvironmentalConditionSnapshot(
                drainage: new CitySystemSnapshot(
                    kind: CitySystemKind.Drainage,
                    loadIndex: drainageLoad,
                    serviceQualityIndex: drainageService,
                    backlogIndex: drainageBacklog,
                    failureRiskIndex: drainageFailureRisk),
                snowRemoval: new CitySystemSnapshot(
                    kind: CitySystemKind.SnowRemoval,
                    loadIndex: snowLoad,
                    serviceQualityIndex: snowService,
                    backlogIndex: snowBacklog,
                    failureRiskIndex: snowFailureRisk),
                roadAccess: new CitySystemSnapshot(
                    kind: CitySystemKind.RoadAccess,
                    loadIndex: roadLoad,
                    serviceQualityIndex: roadService,
                    backlogIndex: roadBacklog,
                    failureRiskIndex: roadFailureRisk),
                floodingIndex: FloodingIndex.From(flooding),
                snowAccumulationIndex: SnowAccumulationIndex.From(snowAccumulation),
                roadAccessibilityIndex: RoadAccessibilityIndex.From(roadAccessibility),
                evaluatedAtUtc: asOfUtc);
        }

        private static decimal Smooth(
            decimal current,
            decimal target,
            decimal factor)
        {
            return decimal.Round(
                d: current + ((target - current) * factor),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal Clamp(
            decimal value,
            decimal min = 0m,
            decimal max = 1m)
        {
            return Math.Min(
                val1: max,
                val2: Math.Max(
                    val1: min,
                    val2: value));
        }

        private static void EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            if (value.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Timestamp must be UTC.",
                    paramName: paramName);
        }
    }
}
