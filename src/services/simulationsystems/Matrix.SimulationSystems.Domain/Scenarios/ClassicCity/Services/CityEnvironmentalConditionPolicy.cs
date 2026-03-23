using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services
{
    /// <summary>
    ///     First-pass environmental condition policy for Classic City.
    ///     Converts weather/system pressure into normalized flooding, snow and access outcomes.
    /// </summary>
    public sealed class CityEnvironmentalConditionPolicy
    {
        public CityEnvironmentalConditionSnapshot CreateSeed(
            Guid cityId,
            string developmentLevel,
            DateTimeOffset asOfUtc)
        {
            EnsureUtc(
                value: asOfUtc,
                paramName: nameof(asOfUtc));

            decimal developmentSeverity = GetDevelopmentSeverity(developmentLevel);

            return new CityEnvironmentalConditionSnapshot(
                drainage: new CitySystemSnapshot(
                    kind: CitySystemKind.Drainage,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-load",
                        baseline: 0.1000m + (developmentSeverity * 0.0900m),
                        maxAbsJitter: 0.0250m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-service",
                        baseline: 0.8400m - (developmentSeverity * 0.1600m),
                        maxAbsJitter: 0.0300m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-backlog",
                        baseline: 0.0700m + (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0300m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-failure",
                        baseline: 0.0400m + (developmentSeverity * 0.0900m),
                        maxAbsJitter: 0.0250m)),
                drainageInfrastructure: new CityDrainageInfrastructureSnapshot(
                    pumpCapacityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-pump-capacity",
                        baseline: 0.9000m - (developmentSeverity * 0.2500m),
                        maxAbsJitter: 0.0350m),
                    networkIntegrityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-network-integrity",
                        baseline: 0.8800m - (developmentSeverity * 0.2200m),
                        maxAbsJitter: 0.0350m),
                    blockageIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-blockage",
                        baseline: 0.0600m + (developmentSeverity * 0.1800m),
                        maxAbsJitter: 0.0400m),
                    crewReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-crew-readiness",
                        baseline: 0.8600m - (developmentSeverity * 0.2100m),
                        maxAbsJitter: 0.0350m),
                    incidentPressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "drainage-incident-pressure",
                        baseline: 0.0500m + (developmentSeverity * 0.1400m),
                        maxAbsJitter: 0.0350m),
                    emergencyModeEnabled: false),
                snowRemoval: new CitySystemSnapshot(
                    kind: CitySystemKind.SnowRemoval,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-load",
                        baseline: 0.0800m + (developmentSeverity * 0.0400m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-service",
                        baseline: 0.8000m - (developmentSeverity * 0.0900m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-backlog",
                        baseline: 0.0500m + (developmentSeverity * 0.0700m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-failure",
                        baseline: 0.0400m + (developmentSeverity * 0.0500m),
                        maxAbsJitter: 0.0200m)),
                roadAccess: new CitySystemSnapshot(
                    kind: CitySystemKind.RoadAccess,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-load",
                        baseline: 0.0900m + (developmentSeverity * 0.0450m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-service",
                        baseline: 0.8500m - (developmentSeverity * 0.0800m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-backlog",
                        baseline: 0.0600m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-failure",
                        baseline: 0.0400m + (developmentSeverity * 0.0500m),
                        maxAbsJitter: 0.0200m)),
                floodingIndex: FloodingIndex.From(CreateSeedMetric(
                    cityId: cityId,
                    salt: "flooding",
                    baseline: 0.0400m + (developmentSeverity * 0.0600m),
                    maxAbsJitter: 0.0150m)),
                snowAccumulationIndex: SnowAccumulationIndex.From(CreateSeedMetric(
                    cityId: cityId,
                    salt: "snow-accumulation",
                    baseline: 0.0200m + (developmentSeverity * 0.0200m),
                    maxAbsJitter: 0.0100m)),
                roadAccessibilityIndex: RoadAccessibilityIndex.From(CreateSeedMetric(
                    cityId: cityId,
                    salt: "road-accessibility",
                    baseline: 0.9600m - (developmentSeverity * 0.0500m),
                    maxAbsJitter: 0.0150m)),
                evaluatedAtUtc: asOfUtc);
        }

        public CityEnvironmentalConditionSnapshot Recalculate(
            CityEnvironmentalConditionState state,
            CitySystemPressureProfile pressure,
            DateTimeOffset asOfUtc)
        {
            return RecalculateCore(
                state: state,
                pressure: pressure,
                asOfUtc: asOfUtc,
                responseScale: 1m);
        }

        public CityEnvironmentalConditionSnapshot Advance(
            CityEnvironmentalConditionState state,
            CitySystemPressureProfile pressure,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc)
        {
            GuardHelper.AgainstNull(
                value: state,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired);
            GuardHelper.AgainstNull(
                value: pressure,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionPressureRequired);
            EnsureUtc(
                value: fromUtc,
                paramName: nameof(fromUtc));
            EnsureUtc(
                value: toUtc,
                paramName: nameof(toUtc));

            if (toUtc < fromUtc)
                throw ClassicCityDomainErrorsFactory.CityEnvironmentalConditionAdvanceWindowInvalid(
                    from: fromUtc,
                    to: toUtc,
                    propertyName: nameof(toUtc));

            decimal responseScale = CalculateAdvanceResponseScale(toUtc - fromUtc);

            if (responseScale <= 0m)
                return state.ToSnapshot();

            return RecalculateCore(
                state: state,
                pressure: pressure,
                asOfUtc: toUtc,
                responseScale: responseScale);
        }

        private static CityEnvironmentalConditionSnapshot RecalculateCore(
            CityEnvironmentalConditionState state,
            CitySystemPressureProfile pressure,
            DateTimeOffset asOfUtc,
            decimal responseScale)
        {
            GuardHelper.AgainstNull(
                value: state,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired);
            GuardHelper.AgainstNull(
                value: pressure,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionPressureRequired);
            EnsureUtc(
                value: asOfUtc,
                paramName: nameof(asOfUtc));

            CitySystemSnapshot currentDrainage = state.Drainage.ToSnapshot();
            CityDrainageInfrastructureSnapshot currentDrainageInfrastructure =
                state.DrainageInfrastructure.ToSnapshot();
            CitySystemSnapshot currentSnowRemoval = state.SnowRemoval.ToSnapshot();
            CitySystemSnapshot currentRoadAccess = state.RoadAccess.ToSnapshot();
            decimal emergencyModeBoost = currentDrainageInfrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;

            decimal drainageLoad = Smooth(
                current: currentDrainage.LoadIndex,
                target: Clamp(
                    value: (pressure.RainPressure * 0.72m) +
                           (pressure.StormPressure * 0.33m) +
                           (state.FloodingIndex.Value * 0.18m) -
                           (pressure.DrainageSupport * 0.28m) -
                           (currentDrainageInfrastructure.PumpCapacityIndex * 0.0600m)),
                factor: 0.45m,
                responseScale: responseScale);
            decimal drainageService = Smooth(
                current: currentDrainage.ServiceQualityIndex,
                target: Clamp(
                    value: 0.62m +
                           (pressure.DrainageSupport * 0.33m) -
                           (currentDrainage.BacklogIndex * 0.22m) -
                           (pressure.StormPressure * 0.08m) +
                           (emergencyModeBoost * 0.45m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal drainageBacklog = Smooth(
                current: currentDrainage.BacklogIndex,
                target: Clamp(
                    value: currentDrainage.BacklogIndex +
                           (drainageLoad * 0.22m) -
                           (drainageService * 0.18m) -
                           (pressure.ThawRelief * 0.05m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal drainageFailureRisk = Smooth(
                current: currentDrainage.FailureRiskIndex,
                target: Clamp(
                    value: (drainageLoad * 0.44m) +
                           (drainageBacklog * 0.34m) +
                           ((1m - drainageService) * 0.30m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal flooding = Smooth(
                current: state.FloodingIndex.Value,
                target: Clamp(
                    value: state.FloodingIndex.Value +
                           (pressure.RainPressure * 0.26m) +
                           (pressure.StormPressure * 0.18m) +
                           (drainageBacklog * 0.14m) -
                           (drainageService * 0.24m) -
                           (pressure.DrainageSupport * 0.08m) -
                           (currentDrainageInfrastructure.BlockageIndex * 0.0400m)),
                factor: 0.42m,
                responseScale: responseScale);

            decimal drainagePumpCapacity = Smooth(
                current: currentDrainageInfrastructure.PumpCapacityIndex,
                target: Clamp(
                    value: currentDrainageInfrastructure.PumpCapacityIndex -
                           (drainageLoad * 0.0600m) -
                           (pressure.StormPressure * 0.0500m) -
                           (currentDrainageInfrastructure.IncidentPressureIndex * 0.0400m) +
                           (currentDrainageInfrastructure.CrewReadinessIndex * 0.0600m) +
                           (emergencyModeBoost * 0.3500m)),
                factor: 0.22m,
                responseScale: responseScale);
            decimal drainageNetworkIntegrity = Smooth(
                current: currentDrainageInfrastructure.NetworkIntegrityIndex,
                target: Clamp(
                    value: currentDrainageInfrastructure.NetworkIntegrityIndex -
                           (drainageBacklog * 0.0500m) -
                           (pressure.StormPressure * 0.0300m) -
                           (flooding * 0.0200m) +
                           (currentDrainageInfrastructure.CrewReadinessIndex * 0.0400m)),
                factor: 0.14m,
                responseScale: responseScale);
            decimal drainageBlockage = Smooth(
                current: currentDrainageInfrastructure.BlockageIndex,
                target: Clamp(
                    value: currentDrainageInfrastructure.BlockageIndex +
                           (pressure.RainPressure * 0.1400m) +
                           (pressure.StormPressure * 0.0600m) +
                           (flooding * 0.0600m) -
                           (currentDrainageInfrastructure.CrewReadinessIndex * 0.0700m) -
                           (pressure.ThawRelief * 0.0400m) -
                           (emergencyModeBoost * 0.8000m)),
                factor: 0.28m,
                responseScale: responseScale);
            decimal drainageCrewReadiness = Smooth(
                current: currentDrainageInfrastructure.CrewReadinessIndex,
                target: Clamp(
                    value: currentDrainageInfrastructure.CrewReadinessIndex +
                           (currentDrainageInfrastructure.EmergencyModeEnabled ? -0.0500m : 0.0250m) -
                           (currentDrainageInfrastructure.IncidentPressureIndex * 0.0300m) -
                           (drainageBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0300m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal drainageIncidentPressure = Smooth(
                current: currentDrainageInfrastructure.IncidentPressureIndex,
                target: Clamp(
                    value: currentDrainageInfrastructure.IncidentPressureIndex +
                           (flooding * 0.1200m) +
                           (drainageFailureRisk * 0.0900m) +
                           (drainageBlockage * 0.0900m) -
                           (drainageCrewReadiness * 0.0600m) -
                           (emergencyModeBoost * 0.4500m)),
                factor: 0.26m,
                responseScale: responseScale);

            decimal snowLoad = Smooth(
                current: currentSnowRemoval.LoadIndex,
                target: Clamp(
                    value: (pressure.SnowPressure * 0.80m) +
                           (pressure.FreezePressure * 0.20m) +
                           (state.SnowAccumulationIndex.Value * 0.22m) -
                           (pressure.SnowRemovalSupport * 0.25m)),
                factor: 0.45m,
                responseScale: responseScale);
            decimal snowService = Smooth(
                current: currentSnowRemoval.ServiceQualityIndex,
                target: Clamp(
                    value: 0.58m +
                           (pressure.SnowRemovalSupport * 0.36m) -
                           (currentSnowRemoval.BacklogIndex * 0.20m) -
                           (pressure.FreezePressure * 0.08m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal snowBacklog = Smooth(
                current: currentSnowRemoval.BacklogIndex,
                target: Clamp(
                    value: currentSnowRemoval.BacklogIndex +
                           (snowLoad * 0.24m) -
                           (snowService * 0.16m) -
                           (pressure.ThawRelief * 0.12m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal snowFailureRisk = Smooth(
                current: currentSnowRemoval.FailureRiskIndex,
                target: Clamp(
                    value: (snowLoad * 0.40m) +
                           (snowBacklog * 0.32m) +
                           ((1m - snowService) * 0.28m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal snowAccumulation = Smooth(
                current: state.SnowAccumulationIndex.Value,
                target: Clamp(
                    value: state.SnowAccumulationIndex.Value +
                           (pressure.SnowPressure * 0.30m) +
                           (pressure.FreezePressure * 0.07m) +
                           (snowBacklog * 0.10m) -
                           (snowService * 0.22m) -
                           (pressure.ThawRelief * 0.20m)),
                factor: 0.42m,
                responseScale: responseScale);

            decimal roadLoad = Smooth(
                current: currentRoadAccess.LoadIndex,
                target: Clamp(
                    value: (flooding * 0.32m) +
                           (snowAccumulation * 0.38m) +
                           (pressure.FreezePressure * 0.14m) +
                           (pressure.StormPressure * 0.10m) -
                           (pressure.RoadSupport * 0.18m)),
                factor: 0.45m,
                responseScale: responseScale);
            decimal roadService = Smooth(
                current: currentRoadAccess.ServiceQualityIndex,
                target: Clamp(
                    value: 0.60m +
                           (pressure.RoadSupport * 0.34m) -
                           (currentRoadAccess.BacklogIndex * 0.18m) -
                           ((snowAccumulation + flooding) * 0.10m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal roadBacklog = Smooth(
                current: currentRoadAccess.BacklogIndex,
                target: Clamp(
                    value: currentRoadAccess.BacklogIndex +
                           (roadLoad * 0.16m) -
                           (roadService * 0.14m) -
                           (pressure.ThawRelief * 0.04m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal roadFailureRisk = Smooth(
                current: currentRoadAccess.FailureRiskIndex,
                target: Clamp(
                    value: (roadLoad * 0.38m) +
                           (roadBacklog * 0.30m) +
                           ((1m - roadService) * 0.25m)),
                factor: 0.30m,
                responseScale: responseScale);

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
                factor: 0.50m,
                responseScale: responseScale);

            return new CityEnvironmentalConditionSnapshot(
                drainage: new CitySystemSnapshot(
                    kind: CitySystemKind.Drainage,
                    loadIndex: drainageLoad,
                    serviceQualityIndex: drainageService,
                    backlogIndex: drainageBacklog,
                    failureRiskIndex: drainageFailureRisk),
                drainageInfrastructure: new CityDrainageInfrastructureSnapshot(
                    pumpCapacityIndex: drainagePumpCapacity,
                    networkIntegrityIndex: drainageNetworkIntegrity,
                    blockageIndex: drainageBlockage,
                    crewReadinessIndex: drainageCrewReadiness,
                    incidentPressureIndex: drainageIncidentPressure,
                    emergencyModeEnabled: currentDrainageInfrastructure.EmergencyModeEnabled),
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

        private static decimal GetDevelopmentSeverity(string developmentLevel)
        {
            return developmentLevel.Trim().ToLowerInvariant() switch
            {
                "struggling" => 1.0000m,
                "advanced" => -0.7500m,
                _ => 0m
            };
        }

        private static decimal CreateSeedMetric(
            Guid cityId,
            string salt,
            decimal baseline,
            decimal maxAbsJitter)
        {
            byte[] hash = SHA256.HashData(
                source: Encoding.UTF8.GetBytes($"{cityId:N}|{salt}"));
            int sample = BitConverter.ToInt32(
                             value: hash,
                             startIndex: 0) &
                         int.MaxValue;

            decimal normalized = sample / (decimal)int.MaxValue;
            decimal centered = (normalized - 0.5m) * 2m;

            return Clamp(
                value: baseline + (centered * maxAbsJitter));
        }

        private static decimal Smooth(
            decimal current,
            decimal target,
            decimal factor,
            decimal responseScale)
        {
            decimal effectiveFactor = CalculateEffectiveFactor(
                baseFactor: factor,
                responseScale: responseScale);

            return decimal.Round(
                d: current + ((target - current) * effectiveFactor),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal CalculateAdvanceResponseScale(TimeSpan elapsed)
        {
            if (elapsed <= TimeSpan.Zero)
                return 0m;

            decimal elapsedMinutes = (decimal)elapsed.TotalMinutes;
            decimal scale = elapsedMinutes / 10m;

            return decimal.Round(
                d: Math.Min(
                    val1: 144m,
                    val2: Math.Max(
                        val1: 0m,
                        val2: scale)),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal CalculateEffectiveFactor(
            decimal baseFactor,
            decimal responseScale)
        {
            if (responseScale <= 0m)
                return 0m;

            decimal normalizedBaseFactor = Clamp(
                value: baseFactor,
                min: 0m,
                max: 1m);

            double scaledFactor = 1d - Math.Pow(
                x: 1d - (double)normalizedBaseFactor,
                y: (double)responseScale);

            return decimal.Round(
                d: Clamp(
                    value: (decimal)scaledFactor,
                    min: 0m,
                    max: 1m),
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
                throw ClassicCityDomainErrorsFactory.CityEnvironmentalTimestampMustBeUtc(
                    value: value,
                    propertyName: paramName);
        }
    }
}
