using System.Security.Cryptography;
using System.Text;
using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;

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
                        baseline: 0.0750m + (developmentSeverity * 0.0500m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-service",
                        baseline: 0.8200m - (developmentSeverity * 0.1200m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-backlog",
                        baseline: 0.0450m + (developmentSeverity * 0.0900m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-failure",
                        baseline: 0.0350m + (developmentSeverity * 0.0700m),
                        maxAbsJitter: 0.0200m)),
                snowRemovalInfrastructure: new CitySnowRemovalInfrastructureSnapshot(
                    fleetAvailabilityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-fleet-availability",
                        baseline: 0.8700m - (developmentSeverity * 0.2300m),
                        maxAbsJitter: 0.0350m),
                    routeCoverageIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-route-coverage",
                        baseline: 0.8500m - (developmentSeverity * 0.2200m),
                        maxAbsJitter: 0.0400m),
                    deicingReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-deicing-readiness",
                        baseline: 0.8400m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    crewReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-crew-readiness",
                        baseline: 0.8300m - (developmentSeverity * 0.2100m),
                        maxAbsJitter: 0.0350m),
                    incidentPressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-incident-pressure",
                        baseline: 0.0500m + (developmentSeverity * 0.1500m),
                        maxAbsJitter: 0.0200m),
                    emergencyModeEnabled: false),
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
                roadAccessInfrastructure: new CityRoadAccessInfrastructureSnapshot(
                    corridorAvailabilityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-corridor-availability",
                        baseline: 0.9000m - (developmentSeverity * 0.2400m),
                        maxAbsJitter: 0.0350m),
                    surfaceIntegrityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-surface-integrity",
                        baseline: 0.8700m - (developmentSeverity * 0.2200m),
                        maxAbsJitter: 0.0350m),
                    trafficControlReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-traffic-control-readiness",
                        baseline: 0.8500m - (developmentSeverity * 0.2100m),
                        maxAbsJitter: 0.0350m),
                    crewReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-crew-readiness",
                        baseline: 0.8400m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    incidentPressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-incident-pressure",
                        baseline: 0.0500m + (developmentSeverity * 0.1400m),
                        maxAbsJitter: 0.0250m),
                    emergencyModeEnabled: false),
                heating: new CitySystemSnapshot(
                    kind: CitySystemKind.Heating,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-load",
                        baseline: 0.1000m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-service",
                        baseline: 0.8600m - (developmentSeverity * 0.1400m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-backlog",
                        baseline: 0.0500m + (developmentSeverity * 0.0900m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-failure",
                        baseline: 0.0350m + (developmentSeverity * 0.0700m),
                        maxAbsJitter: 0.0200m)),
                heatingInfrastructure: new CityHeatingInfrastructureSnapshot(
                    plantCapacityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-plant-capacity",
                        baseline: 0.9000m - (developmentSeverity * 0.2400m),
                        maxAbsJitter: 0.0350m),
                    networkIntegrityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-network-integrity",
                        baseline: 0.8800m - (developmentSeverity * 0.2200m),
                        maxAbsJitter: 0.0350m),
                    controlReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-control-readiness",
                        baseline: 0.8500m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    crewReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-crew-readiness",
                        baseline: 0.8400m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    incidentPressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-incident-pressure",
                        baseline: 0.0450m + (developmentSeverity * 0.1400m),
                        maxAbsJitter: 0.0250m),
                    emergencyModeEnabled: false),
                waterDistribution: new CitySystemSnapshot(
                    kind: CitySystemKind.WaterDistribution,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-load",
                        baseline: 0.0850m + (developmentSeverity * 0.0550m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-service",
                        baseline: 0.8700m - (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-backlog",
                        baseline: 0.0450m + (developmentSeverity * 0.0800m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-failure",
                        baseline: 0.0300m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0200m)),
                waterDistributionInfrastructure: new CityWaterDistributionInfrastructureSnapshot(
                    treatmentCapacityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-treatment-capacity",
                        baseline: 0.9000m - (developmentSeverity * 0.2300m),
                        maxAbsJitter: 0.0350m),
                    networkIntegrityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-network-integrity",
                        baseline: 0.8900m - (developmentSeverity * 0.2200m),
                        maxAbsJitter: 0.0350m),
                    pumpReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-pump-readiness",
                        baseline: 0.8700m - (developmentSeverity * 0.2100m),
                        maxAbsJitter: 0.0350m),
                    crewReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-crew-readiness",
                        baseline: 0.8500m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    incidentPressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-incident-pressure",
                        baseline: 0.0400m + (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0250m),
                    emergencyModeEnabled: false),
                sanitation: new CitySystemSnapshot(
                    kind: CitySystemKind.Sanitation,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-load",
                        baseline: 0.0800m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-service",
                        baseline: 0.8600m - (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-backlog",
                        baseline: 0.0500m + (developmentSeverity * 0.0900m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-failure",
                        baseline: 0.0300m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0200m)),
                sanitationInfrastructure: new CitySanitationInfrastructureSnapshot(
                    treatmentStabilityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-treatment-stability",
                        baseline: 0.8900m - (developmentSeverity * 0.2200m),
                        maxAbsJitter: 0.0350m),
                    networkIntegrityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-network-integrity",
                        baseline: 0.8800m - (developmentSeverity * 0.2100m),
                        maxAbsJitter: 0.0350m),
                    overflowControlIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-overflow-control",
                        baseline: 0.8600m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    crewReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-crew-readiness",
                        baseline: 0.8400m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    incidentPressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-incident-pressure",
                        baseline: 0.0400m + (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0250m),
                    emergencyModeEnabled: false),
                powerDistribution: new CitySystemSnapshot(
                    kind: CitySystemKind.PowerDistribution,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-load",
                        baseline: 0.0950m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-service",
                        baseline: 0.8800m - (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-backlog",
                        baseline: 0.0400m + (developmentSeverity * 0.0800m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-failure",
                        baseline: 0.0300m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0200m)),
                powerDistributionInfrastructure: new CityPowerDistributionInfrastructureSnapshot(
                    substationCapacityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-substation-capacity",
                        baseline: 0.9100m - (developmentSeverity * 0.2300m),
                        maxAbsJitter: 0.0350m),
                    gridIntegrityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-grid-integrity",
                        baseline: 0.8900m - (developmentSeverity * 0.2200m),
                        maxAbsJitter: 0.0350m),
                    switchingReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-switching-readiness",
                        baseline: 0.8600m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    crewReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-crew-readiness",
                        baseline: 0.8400m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    incidentPressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-incident-pressure",
                        baseline: 0.0400m + (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0250m),
                    emergencyModeEnabled: false),
                utilityIncidents: new CitySystemSnapshot(
                    kind: CitySystemKind.UtilityIncidents,
                    loadIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-load",
                        baseline: 0.0700m + (developmentSeverity * 0.0500m),
                        maxAbsJitter: 0.0200m),
                    serviceQualityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-service",
                        baseline: 0.8500m - (developmentSeverity * 0.1200m),
                        maxAbsJitter: 0.0250m),
                    backlogIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-backlog",
                        baseline: 0.0400m + (developmentSeverity * 0.0700m),
                        maxAbsJitter: 0.0200m),
                    failureRiskIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-failure",
                        baseline: 0.0300m + (developmentSeverity * 0.0500m),
                        maxAbsJitter: 0.0200m)),
                utilityIncidentInfrastructure: new CityUtilityIncidentInfrastructureSnapshot(
                    dispatchReadinessIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-dispatch-readiness",
                        baseline: 0.8600m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    restorationCoverageIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-restoration-coverage",
                        baseline: 0.8700m - (developmentSeverity * 0.2100m),
                        maxAbsJitter: 0.0350m),
                    spareCapacityIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-spare-capacity",
                        baseline: 0.8300m - (developmentSeverity * 0.1900m),
                        maxAbsJitter: 0.0350m),
                    fieldCoordinationIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-field-coordination",
                        baseline: 0.8500m - (developmentSeverity * 0.2000m),
                        maxAbsJitter: 0.0350m),
                    incidentQueuePressureIndex: CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-queue-pressure",
                        baseline: 0.0400m + (developmentSeverity * 0.1300m),
                        maxAbsJitter: 0.0250m),
                    emergencyModeEnabled: false),
                floodingIndex: FloodingIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "flooding",
                        baseline: 0.0400m + (developmentSeverity * 0.0600m),
                        maxAbsJitter: 0.0150m)),
                snowAccumulationIndex: SnowAccumulationIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "snow-accumulation",
                        baseline: 0.0200m + (developmentSeverity * 0.0200m),
                        maxAbsJitter: 0.0100m)),
                roadAccessibilityIndex: RoadAccessibilityIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "road-accessibility",
                        baseline: 0.9600m - (developmentSeverity * 0.0500m),
                        maxAbsJitter: 0.0150m)),
                heatingCoverageIndex: HeatingCoverageIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "heating-coverage",
                        baseline: 0.9300m - (developmentSeverity * 0.1200m),
                        maxAbsJitter: 0.0200m)),
                waterCoverageIndex: WaterCoverageIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "water-distribution-coverage",
                        baseline: 0.9500m - (developmentSeverity * 0.1000m),
                        maxAbsJitter: 0.0200m)),
                sanitationCoverageIndex: SanitationCoverageIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "sanitation-coverage",
                        baseline: 0.9400m - (developmentSeverity * 0.1100m),
                        maxAbsJitter: 0.0200m)),
                powerCoverageIndex: PowerCoverageIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "power-distribution-coverage",
                        baseline: 0.9600m - (developmentSeverity * 0.1000m),
                        maxAbsJitter: 0.0200m)),
                utilityContinuityIndex: UtilityContinuityIndex.From(
                    CreateSeedMetric(
                        cityId: cityId,
                        salt: "utility-incidents-continuity",
                        baseline: 0.9500m - (developmentSeverity * 0.0900m),
                        maxAbsJitter: 0.0200m)),
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
            CitySnowRemovalInfrastructureSnapshot currentSnowRemovalInfrastructure =
                state.SnowRemovalInfrastructure.ToSnapshot();
            CitySystemSnapshot currentRoadAccess = state.RoadAccess.ToSnapshot();
            CityRoadAccessInfrastructureSnapshot currentRoadAccessInfrastructure =
                state.RoadAccessInfrastructure.ToSnapshot();
            CitySystemSnapshot currentHeating = state.Heating.ToSnapshot();
            CityHeatingInfrastructureSnapshot currentHeatingInfrastructure =
                state.HeatingInfrastructure.ToSnapshot();
            CitySystemSnapshot currentWaterDistribution = state.WaterDistribution.ToSnapshot();
            CityWaterDistributionInfrastructureSnapshot currentWaterDistributionInfrastructure =
                state.WaterDistributionInfrastructure.ToSnapshot();
            CitySystemSnapshot currentSanitation = state.Sanitation.ToSnapshot();
            CitySanitationInfrastructureSnapshot currentSanitationInfrastructure =
                state.SanitationInfrastructure.ToSnapshot();
            CitySystemSnapshot currentPowerDistribution = state.PowerDistribution.ToSnapshot();
            CityPowerDistributionInfrastructureSnapshot currentPowerDistributionInfrastructure =
                state.PowerDistributionInfrastructure.ToSnapshot();
            CitySystemSnapshot currentUtilityIncidents = state.UtilityIncidents.ToSnapshot();
            CityUtilityIncidentInfrastructureSnapshot currentUtilityIncidentInfrastructure =
                state.UtilityIncidentInfrastructure.ToSnapshot();
            decimal emergencyModeBoost = currentDrainageInfrastructure.EmergencyModeEnabled
                ? 0.0800m
                : 0m;
            decimal snowEmergencyModeBoost = currentSnowRemovalInfrastructure.EmergencyModeEnabled
                ? 0.0900m
                : 0m;
            decimal roadEmergencyModeBoost = currentRoadAccessInfrastructure.EmergencyModeEnabled
                ? 0.0850m
                : 0m;
            decimal heatingEmergencyModeBoost = currentHeatingInfrastructure.EmergencyModeEnabled
                ? 0.0900m
                : 0m;
            decimal waterEmergencyModeBoost = currentWaterDistributionInfrastructure.EmergencyModeEnabled
                ? 0.0850m
                : 0m;
            decimal sanitationEmergencyModeBoost = currentSanitationInfrastructure.EmergencyModeEnabled
                ? 0.0850m
                : 0m;
            decimal powerEmergencyModeBoost = currentPowerDistributionInfrastructure.EmergencyModeEnabled
                ? 0.0900m
                : 0m;
            decimal utilityIncidentEmergencyModeBoost = currentUtilityIncidentInfrastructure.EmergencyModeEnabled
                ? 0.0900m
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
                           (currentDrainageInfrastructure.EmergencyModeEnabled
                               ? -0.0500m
                               : 0.0250m) -
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
                           (pressure.FreezePressure * 0.18m) +
                           (state.SnowAccumulationIndex.Value * 0.22m) -
                           (pressure.SnowRemovalSupport * 0.22m) -
                           (currentSnowRemovalInfrastructure.RouteCoverageIndex * 0.0600m) -
                           (currentSnowRemovalInfrastructure.DeicingReadinessIndex * 0.0400m) +
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.0800m)),
                factor: 0.45m,
                responseScale: responseScale);
            decimal snowService = Smooth(
                current: currentSnowRemoval.ServiceQualityIndex,
                target: Clamp(
                    value: 0.58m +
                           (pressure.SnowRemovalSupport * 0.32m) +
                           (currentSnowRemovalInfrastructure.RouteCoverageIndex * 0.0600m) +
                           (currentSnowRemovalInfrastructure.DeicingReadinessIndex * 0.0500m) -
                           (currentSnowRemoval.BacklogIndex * 0.18m) -
                           (pressure.FreezePressure * 0.08m) +
                           (snowEmergencyModeBoost * 0.4200m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal snowBacklog = Smooth(
                current: currentSnowRemoval.BacklogIndex,
                target: Clamp(
                    value: currentSnowRemoval.BacklogIndex +
                           (snowLoad * 0.22m) -
                           (snowService * 0.16m) -
                           (pressure.ThawRelief * 0.12m) -
                           (currentSnowRemovalInfrastructure.RouteCoverageIndex * 0.0400m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal snowFailureRisk = Smooth(
                current: currentSnowRemoval.FailureRiskIndex,
                target: Clamp(
                    value: (snowLoad * 0.38m) +
                           (snowBacklog * 0.30m) +
                           ((1m - snowService) * 0.26m) +
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.14m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal snowAccumulation = Smooth(
                current: state.SnowAccumulationIndex.Value,
                target: Clamp(
                    value: state.SnowAccumulationIndex.Value +
                           (pressure.SnowPressure * 0.30m) +
                           (pressure.FreezePressure * 0.08m) +
                           (snowBacklog * 0.10m) -
                           (snowService * 0.20m) -
                           (pressure.SnowRemovalSupport * 0.06m) -
                           (currentSnowRemovalInfrastructure.DeicingReadinessIndex * 0.0500m) -
                           (pressure.ThawRelief * 0.22m) -
                           (snowEmergencyModeBoost * 0.1800m) +
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.0500m)),
                factor: 0.42m,
                responseScale: responseScale);

            decimal snowFleetAvailability = Smooth(
                current: currentSnowRemovalInfrastructure.FleetAvailabilityIndex,
                target: Clamp(
                    value: currentSnowRemovalInfrastructure.FleetAvailabilityIndex -
                           (snowLoad * 0.0550m) -
                           (pressure.StormPressure * 0.0250m) -
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.0450m) +
                           (currentSnowRemovalInfrastructure.CrewReadinessIndex * 0.0600m) +
                           (snowEmergencyModeBoost * 0.3200m)),
                factor: 0.22m,
                responseScale: responseScale);
            decimal snowRouteCoverage = Smooth(
                current: currentSnowRemovalInfrastructure.RouteCoverageIndex,
                target: Clamp(
                    value: currentSnowRemovalInfrastructure.RouteCoverageIndex -
                           (snowAccumulation * 0.0900m) -
                           (snowBacklog * 0.0500m) -
                           (pressure.FreezePressure * 0.0300m) +
                           (snowFleetAvailability * 0.0700m) +
                           (currentSnowRemovalInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (snowEmergencyModeBoost * 0.2800m) +
                           (pressure.ThawRelief * 0.0300m)),
                factor: 0.26m,
                responseScale: responseScale);
            decimal snowDeicingReadiness = Smooth(
                current: currentSnowRemovalInfrastructure.DeicingReadinessIndex,
                target: Clamp(
                    value: currentSnowRemovalInfrastructure.DeicingReadinessIndex -
                           (pressure.FreezePressure * 0.0600m) -
                           (snowLoad * 0.0300m) -
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.0300m) +
                           (currentSnowRemovalInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.0500m) +
                           (snowEmergencyModeBoost * 0.1800m)),
                factor: 0.20m,
                responseScale: responseScale);
            decimal snowCrewReadiness = Smooth(
                current: currentSnowRemovalInfrastructure.CrewReadinessIndex,
                target: Clamp(
                    value: currentSnowRemovalInfrastructure.CrewReadinessIndex +
                           (currentSnowRemovalInfrastructure.EmergencyModeEnabled
                               ? -0.0550m
                               : 0.0280m) -
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.0320m) -
                           (snowBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0300m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal snowIncidentPressure = Smooth(
                current: currentSnowRemovalInfrastructure.IncidentPressureIndex,
                target: Clamp(
                    value: currentSnowRemovalInfrastructure.IncidentPressureIndex +
                           (snowAccumulation * 0.1200m) +
                           (snowFailureRisk * 0.0900m) +
                           ((1m - snowRouteCoverage) * 0.0800m) -
                           (snowCrewReadiness * 0.0600m) -
                           (snowEmergencyModeBoost * 0.4300m)),
                factor: 0.26m,
                responseScale: responseScale);

            decimal roadLoad = Smooth(
                current: currentRoadAccess.LoadIndex,
                target: Clamp(
                    value: (flooding * 0.30m) +
                           (snowAccumulation * 0.36m) +
                           (pressure.FreezePressure * 0.12m) +
                           (pressure.StormPressure * 0.10m) -
                           (pressure.RoadSupport * 0.18m) -
                           (currentSnowRemovalInfrastructure.RouteCoverageIndex * 0.0800m) -
                           (currentSnowRemovalInfrastructure.DeicingReadinessIndex * 0.0500m) -
                           (currentRoadAccessInfrastructure.CorridorAvailabilityIndex * 0.0800m) -
                           (currentRoadAccessInfrastructure.TrafficControlReadinessIndex * 0.0600m) +
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.0800m) +
                           (currentRoadAccessInfrastructure.IncidentPressureIndex * 0.0900m)),
                factor: 0.45m,
                responseScale: responseScale);
            decimal roadService = Smooth(
                current: currentRoadAccess.ServiceQualityIndex,
                target: Clamp(
                    value: 0.58m +
                           (pressure.RoadSupport * 0.30m) +
                           (currentSnowRemovalInfrastructure.RouteCoverageIndex * 0.0900m) +
                           (currentSnowRemovalInfrastructure.DeicingReadinessIndex * 0.0600m) +
                           (currentRoadAccessInfrastructure.CorridorAvailabilityIndex * 0.0700m) +
                           (currentRoadAccessInfrastructure.SurfaceIntegrityIndex * 0.0600m) +
                           (currentRoadAccessInfrastructure.TrafficControlReadinessIndex * 0.0600m) -
                           (currentRoadAccess.BacklogIndex * 0.18m) -
                           ((snowAccumulation + flooding) * 0.10m) +
                           (roadEmergencyModeBoost * 0.3800m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal roadBacklog = Smooth(
                current: currentRoadAccess.BacklogIndex,
                target: Clamp(
                    value: currentRoadAccess.BacklogIndex +
                           (roadLoad * 0.18m) -
                           (roadService * 0.14m) -
                           (pressure.ThawRelief * 0.04m) -
                           (currentRoadAccessInfrastructure.CrewReadinessIndex * 0.0400m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal roadFailureRisk = Smooth(
                current: currentRoadAccess.FailureRiskIndex,
                target: Clamp(
                    value: (roadLoad * 0.36m) +
                           (roadBacklog * 0.30m) +
                           ((1m - roadService) * 0.25m) +
                           (currentRoadAccessInfrastructure.IncidentPressureIndex * 0.1400m) +
                           ((1m - currentRoadAccessInfrastructure.SurfaceIntegrityIndex) * 0.1200m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal roadAccessibility = Smooth(
                current: state.RoadAccessibilityIndex.Value,
                target: Clamp(
                    value: 1.03m -
                           (flooding * 0.36m) -
                           (snowAccumulation * 0.38m) -
                           (pressure.FreezePressure * 0.10m) -
                           (roadBacklog * 0.14m) +
                           (roadService * 0.10m) +
                           (pressure.ThawRelief * 0.06m) +
                           (currentSnowRemovalInfrastructure.RouteCoverageIndex * 0.1000m) +
                           (currentSnowRemovalInfrastructure.DeicingReadinessIndex * 0.0600m) -
                           (currentSnowRemovalInfrastructure.IncidentPressureIndex * 0.0600m) +
                           (currentRoadAccessInfrastructure.CorridorAvailabilityIndex * 0.1400m) +
                           (currentRoadAccessInfrastructure.TrafficControlReadinessIndex * 0.0800m) -
                           (currentRoadAccessInfrastructure.IncidentPressureIndex * 0.0700m),
                    min: 0.15m,
                    max: 1m),
                factor: 0.50m,
                responseScale: responseScale);

            decimal roadCorridorAvailability = Smooth(
                current: currentRoadAccessInfrastructure.CorridorAvailabilityIndex,
                target: Clamp(
                    value: currentRoadAccessInfrastructure.CorridorAvailabilityIndex -
                           (flooding * 0.0800m) -
                           (snowAccumulation * 0.0700m) -
                           (roadBacklog * 0.0500m) -
                           (currentRoadAccessInfrastructure.IncidentPressureIndex * 0.0400m) +
                           (currentRoadAccessInfrastructure.SurfaceIntegrityIndex * 0.0500m) +
                           (currentRoadAccessInfrastructure.CrewReadinessIndex * 0.0600m) +
                           (roadEmergencyModeBoost * 0.2800m) +
                           (currentSnowRemovalInfrastructure.RouteCoverageIndex * 0.0600m) +
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.24m,
                responseScale: responseScale);
            decimal roadSurfaceIntegrity = Smooth(
                current: currentRoadAccessInfrastructure.SurfaceIntegrityIndex,
                target: Clamp(
                    value: currentRoadAccessInfrastructure.SurfaceIntegrityIndex -
                           (roadLoad * 0.0500m) -
                           (pressure.FreezePressure * 0.0400m) -
                           (flooding * 0.0300m) -
                           (currentRoadAccessInfrastructure.IncidentPressureIndex * 0.0300m) +
                           (currentRoadAccessInfrastructure.CrewReadinessIndex * 0.0400m) +
                           (roadEmergencyModeBoost * 0.1000m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal roadTrafficControlReadiness = Smooth(
                current: currentRoadAccessInfrastructure.TrafficControlReadinessIndex,
                target: Clamp(
                    value: currentRoadAccessInfrastructure.TrafficControlReadinessIndex -
                           (pressure.StormPressure * 0.0400m) -
                           (currentRoadAccessInfrastructure.IncidentPressureIndex * 0.0300m) -
                           (roadLoad * 0.0200m) +
                           (currentRoadAccessInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.0300m) +
                           (roadEmergencyModeBoost * 0.2000m)),
                factor: 0.18m,
                responseScale: responseScale);
            decimal roadCrewReadiness = Smooth(
                current: currentRoadAccessInfrastructure.CrewReadinessIndex,
                target: Clamp(
                    value: currentRoadAccessInfrastructure.CrewReadinessIndex +
                           (currentRoadAccessInfrastructure.EmergencyModeEnabled
                               ? -0.0500m
                               : 0.0280m) -
                           (currentRoadAccessInfrastructure.IncidentPressureIndex * 0.0320m) -
                           (roadBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal roadIncidentPressure = Smooth(
                current: currentRoadAccessInfrastructure.IncidentPressureIndex,
                target: Clamp(
                    value: currentRoadAccessInfrastructure.IncidentPressureIndex +
                           ((1m - roadAccessibility) * 0.1400m) +
                           (roadFailureRisk * 0.0900m) +
                           (flooding * 0.0700m) +
                           (snowAccumulation * 0.0600m) -
                           (roadCrewReadiness * 0.0600m) -
                           (roadTrafficControlReadiness * 0.0400m) -
                           (roadEmergencyModeBoost * 0.4300m)),
                factor: 0.26m,
                responseScale: responseScale);

            decimal heatingLoad = Smooth(
                current: currentHeating.LoadIndex,
                target: Clamp(
                    value: (pressure.FreezePressure * 0.78m) +
                           (pressure.StormPressure * 0.12m) +
                           ((1m - state.HeatingCoverageIndex.Value) * 0.20m) -
                           (pressure.HeatingSupport * 0.24m) -
                           (currentHeatingInfrastructure.PlantCapacityIndex * 0.0700m)),
                factor: 0.45m,
                responseScale: responseScale);
            decimal heatingService = Smooth(
                current: currentHeating.ServiceQualityIndex,
                target: Clamp(
                    value: 0.60m +
                           (pressure.HeatingSupport * 0.30m) +
                           (currentHeatingInfrastructure.ControlReadinessIndex * 0.0700m) -
                           (currentHeating.BacklogIndex * 0.20m) -
                           (pressure.FreezePressure * 0.10m) +
                           (heatingEmergencyModeBoost * 0.4200m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal heatingBacklog = Smooth(
                current: currentHeating.BacklogIndex,
                target: Clamp(
                    value: currentHeating.BacklogIndex +
                           (heatingLoad * 0.20m) -
                           (heatingService * 0.16m) -
                           (pressure.ThawRelief * 0.06m) -
                           (currentHeatingInfrastructure.CrewReadinessIndex * 0.0400m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal heatingFailureRisk = Smooth(
                current: currentHeating.FailureRiskIndex,
                target: Clamp(
                    value: (heatingLoad * 0.38m) +
                           (heatingBacklog * 0.30m) +
                           ((1m - heatingService) * 0.28m) +
                           (currentHeatingInfrastructure.IncidentPressureIndex * 0.1400m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal heatingCoverage = Smooth(
                current: state.HeatingCoverageIndex.Value,
                target: Clamp(
                    value: 1.01m -
                           (pressure.FreezePressure * 0.26m) -
                           (heatingBacklog * 0.18m) -
                           (heatingFailureRisk * 0.12m) +
                           (heatingService * 0.12m) +
                           (pressure.HeatingSupport * 0.08m) +
                           (currentHeatingInfrastructure.PlantCapacityIndex * 0.0700m) +
                           (pressure.ThawRelief * 0.04m) -
                           (currentHeatingInfrastructure.IncidentPressureIndex * 0.0600m),
                    min: 0.10m,
                    max: 1m),
                factor: 0.46m,
                responseScale: responseScale);

            decimal heatingPlantCapacity = Smooth(
                current: currentHeatingInfrastructure.PlantCapacityIndex,
                target: Clamp(
                    value: currentHeatingInfrastructure.PlantCapacityIndex -
                           (heatingLoad * 0.0550m) -
                           (pressure.StormPressure * 0.0300m) -
                           (currentHeatingInfrastructure.IncidentPressureIndex * 0.0400m) +
                           (currentHeatingInfrastructure.CrewReadinessIndex * 0.0600m) +
                           (heatingEmergencyModeBoost * 0.3000m)),
                factor: 0.22m,
                responseScale: responseScale);
            decimal heatingNetworkIntegrity = Smooth(
                current: currentHeatingInfrastructure.NetworkIntegrityIndex,
                target: Clamp(
                    value: currentHeatingInfrastructure.NetworkIntegrityIndex -
                           (heatingBacklog * 0.0500m) -
                           (pressure.FreezePressure * 0.0400m) -
                           (pressure.StormPressure * 0.0250m) +
                           (currentHeatingInfrastructure.CrewReadinessIndex * 0.0400m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal heatingControlReadiness = Smooth(
                current: currentHeatingInfrastructure.ControlReadinessIndex,
                target: Clamp(
                    value: currentHeatingInfrastructure.ControlReadinessIndex -
                           (heatingLoad * 0.0200m) -
                           (currentHeatingInfrastructure.IncidentPressureIndex * 0.0300m) +
                           (currentHeatingInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.0200m) +
                           (heatingEmergencyModeBoost * 0.1800m)),
                factor: 0.18m,
                responseScale: responseScale);
            decimal heatingCrewReadiness = Smooth(
                current: currentHeatingInfrastructure.CrewReadinessIndex,
                target: Clamp(
                    value: currentHeatingInfrastructure.CrewReadinessIndex +
                           (currentHeatingInfrastructure.EmergencyModeEnabled
                               ? -0.0550m
                               : 0.0260m) -
                           (currentHeatingInfrastructure.IncidentPressureIndex * 0.0320m) -
                           (heatingBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal heatingIncidentPressure = Smooth(
                current: currentHeatingInfrastructure.IncidentPressureIndex,
                target: Clamp(
                    value: currentHeatingInfrastructure.IncidentPressureIndex +
                           ((1m - heatingCoverage) * 0.1300m) +
                           (heatingFailureRisk * 0.0900m) +
                           (pressure.FreezePressure * 0.0600m) -
                           (heatingCrewReadiness * 0.0600m) -
                           (heatingControlReadiness * 0.0400m) -
                           (heatingEmergencyModeBoost * 0.4200m)),
                factor: 0.26m,
                responseScale: responseScale);

            decimal waterLoad = Smooth(
                current: currentWaterDistribution.LoadIndex,
                target: Clamp(
                    value: (pressure.FreezePressure * 0.38m) +
                           (pressure.StormPressure * 0.20m) +
                           (state.FloodingIndex.Value * 0.22m) +
                           ((1m - state.WaterCoverageIndex.Value) * 0.18m) +
                           (pressure.RainPressure * 0.08m) -
                           (pressure.WaterSupport * 0.24m) -
                           (currentWaterDistributionInfrastructure.TreatmentCapacityIndex * 0.0600m) -
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.42m,
                responseScale: responseScale);
            decimal waterService = Smooth(
                current: currentWaterDistribution.ServiceQualityIndex,
                target: Clamp(
                    value: 0.63m +
                           (pressure.WaterSupport * 0.30m) +
                           (currentWaterDistributionInfrastructure.PumpReadinessIndex * 0.0600m) -
                           (currentWaterDistribution.BacklogIndex * 0.20m) -
                           (state.FloodingIndex.Value * 0.08m) -
                           (pressure.FreezePressure * 0.06m) +
                           (waterEmergencyModeBoost * 0.4200m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal waterBacklog = Smooth(
                current: currentWaterDistribution.BacklogIndex,
                target: Clamp(
                    value: currentWaterDistribution.BacklogIndex +
                           (waterLoad * 0.18m) -
                           (waterService * 0.15m) -
                           (pressure.ThawRelief * 0.04m) -
                           (currentWaterDistributionInfrastructure.CrewReadinessIndex * 0.0300m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal waterFailureRisk = Smooth(
                current: currentWaterDistribution.FailureRiskIndex,
                target: Clamp(
                    value: (waterLoad * 0.36m) +
                           (waterBacklog * 0.30m) +
                           ((1m - waterService) * 0.26m) +
                           (currentWaterDistributionInfrastructure.IncidentPressureIndex * 0.1400m) +
                           ((1m - currentWaterDistributionInfrastructure.NetworkIntegrityIndex) * 0.1200m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal waterCoverage = Smooth(
                current: state.WaterCoverageIndex.Value,
                target: Clamp(
                    value: 1.02m -
                           (state.FloodingIndex.Value * 0.20m) -
                           (pressure.FreezePressure * 0.16m) -
                           (waterBacklog * 0.16m) -
                           (waterFailureRisk * 0.10m) +
                           (waterService * 0.10m) +
                           (pressure.WaterSupport * 0.10m) +
                           (currentWaterDistributionInfrastructure.TreatmentCapacityIndex * 0.0800m) +
                           (currentWaterDistributionInfrastructure.PumpReadinessIndex * 0.0600m) +
                           (pressure.ThawRelief * 0.03m) -
                           (currentWaterDistributionInfrastructure.IncidentPressureIndex * 0.0600m),
                    min: 0.10m,
                    max: 1m),
                factor: 0.46m,
                responseScale: responseScale);

            decimal waterTreatmentCapacity = Smooth(
                current: currentWaterDistributionInfrastructure.TreatmentCapacityIndex,
                target: Clamp(
                    value: currentWaterDistributionInfrastructure.TreatmentCapacityIndex -
                           (waterLoad * 0.0450m) -
                           (state.FloodingIndex.Value * 0.0250m) -
                           (currentWaterDistributionInfrastructure.IncidentPressureIndex * 0.0400m) +
                           (currentWaterDistributionInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (waterEmergencyModeBoost * 0.2800m)),
                factor: 0.20m,
                responseScale: responseScale);
            decimal waterNetworkIntegrity = Smooth(
                current: currentWaterDistributionInfrastructure.NetworkIntegrityIndex,
                target: Clamp(
                    value: currentWaterDistributionInfrastructure.NetworkIntegrityIndex -
                           (waterBacklog * 0.0450m) -
                           (pressure.FreezePressure * 0.0400m) -
                           (state.FloodingIndex.Value * 0.0350m) -
                           (pressure.StormPressure * 0.0200m) +
                           (currentWaterDistributionInfrastructure.CrewReadinessIndex * 0.0400m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal waterPumpReadiness = Smooth(
                current: currentWaterDistributionInfrastructure.PumpReadinessIndex,
                target: Clamp(
                    value: currentWaterDistributionInfrastructure.PumpReadinessIndex -
                           (waterLoad * 0.0300m) -
                           (state.FloodingIndex.Value * 0.0400m) -
                           (currentWaterDistributionInfrastructure.IncidentPressureIndex * 0.0300m) +
                           (currentWaterDistributionInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.0200m) +
                           (waterEmergencyModeBoost * 0.1800m)),
                factor: 0.18m,
                responseScale: responseScale);
            decimal waterCrewReadiness = Smooth(
                current: currentWaterDistributionInfrastructure.CrewReadinessIndex,
                target: Clamp(
                    value: currentWaterDistributionInfrastructure.CrewReadinessIndex +
                           (currentWaterDistributionInfrastructure.EmergencyModeEnabled
                               ? -0.0500m
                               : 0.0260m) -
                           (currentWaterDistributionInfrastructure.IncidentPressureIndex * 0.0300m) -
                           (waterBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal waterIncidentPressure = Smooth(
                current: currentWaterDistributionInfrastructure.IncidentPressureIndex,
                target: Clamp(
                    value: currentWaterDistributionInfrastructure.IncidentPressureIndex +
                           ((1m - waterCoverage) * 0.1200m) +
                           (waterFailureRisk * 0.0800m) +
                           (state.FloodingIndex.Value * 0.0700m) +
                           (pressure.FreezePressure * 0.0500m) -
                           (waterCrewReadiness * 0.0600m) -
                           (waterPumpReadiness * 0.0400m) -
                           (waterEmergencyModeBoost * 0.4000m)),
                factor: 0.26m,
                responseScale: responseScale);

            decimal sanitationLoad = Smooth(
                current: currentSanitation.LoadIndex,
                target: Clamp(
                    value: (state.FloodingIndex.Value * 0.28m) +
                           (pressure.StormPressure * 0.18m) +
                           (pressure.FreezePressure * 0.12m) +
                           (pressure.RainPressure * 0.10m) +
                           ((1m - state.SanitationCoverageIndex.Value) * 0.18m) -
                           (pressure.SanitationSupport * 0.24m) -
                           (currentSanitationInfrastructure.TreatmentStabilityIndex * 0.0500m) -
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.42m,
                responseScale: responseScale);
            decimal sanitationService = Smooth(
                current: currentSanitation.ServiceQualityIndex,
                target: Clamp(
                    value: 0.62m +
                           (pressure.SanitationSupport * 0.30m) +
                           (currentSanitationInfrastructure.OverflowControlIndex * 0.0700m) -
                           (currentSanitation.BacklogIndex * 0.20m) -
                           (state.FloodingIndex.Value * 0.08m) -
                           (pressure.StormPressure * 0.06m) +
                           (sanitationEmergencyModeBoost * 0.4200m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal sanitationBacklog = Smooth(
                current: currentSanitation.BacklogIndex,
                target: Clamp(
                    value: currentSanitation.BacklogIndex +
                           (sanitationLoad * 0.18m) -
                           (sanitationService * 0.15m) -
                           (pressure.ThawRelief * 0.04m) -
                           (currentSanitationInfrastructure.CrewReadinessIndex * 0.0300m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal sanitationFailureRisk = Smooth(
                current: currentSanitation.FailureRiskIndex,
                target: Clamp(
                    value: (sanitationLoad * 0.36m) +
                           (sanitationBacklog * 0.30m) +
                           ((1m - sanitationService) * 0.26m) +
                           (currentSanitationInfrastructure.IncidentPressureIndex * 0.1400m) +
                           ((1m - currentSanitationInfrastructure.NetworkIntegrityIndex) * 0.1200m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal sanitationCoverage = Smooth(
                current: state.SanitationCoverageIndex.Value,
                target: Clamp(
                    value: 1.01m -
                           (state.FloodingIndex.Value * 0.22m) -
                           (pressure.StormPressure * 0.12m) -
                           (pressure.FreezePressure * 0.10m) -
                           (sanitationBacklog * 0.15m) -
                           (sanitationFailureRisk * 0.10m) +
                           (sanitationService * 0.10m) +
                           (pressure.SanitationSupport * 0.09m) +
                           (currentSanitationInfrastructure.TreatmentStabilityIndex * 0.0700m) +
                           (currentSanitationInfrastructure.OverflowControlIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.03m) -
                           (currentSanitationInfrastructure.IncidentPressureIndex * 0.0600m),
                    min: 0.10m,
                    max: 1m),
                factor: 0.46m,
                responseScale: responseScale);

            decimal sanitationTreatmentStability = Smooth(
                current: currentSanitationInfrastructure.TreatmentStabilityIndex,
                target: Clamp(
                    value: currentSanitationInfrastructure.TreatmentStabilityIndex -
                           (sanitationLoad * 0.0450m) -
                           (state.FloodingIndex.Value * 0.0300m) -
                           (currentSanitationInfrastructure.IncidentPressureIndex * 0.0400m) +
                           (currentSanitationInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (sanitationEmergencyModeBoost * 0.2800m)),
                factor: 0.20m,
                responseScale: responseScale);
            decimal sanitationNetworkIntegrity = Smooth(
                current: currentSanitationInfrastructure.NetworkIntegrityIndex,
                target: Clamp(
                    value: currentSanitationInfrastructure.NetworkIntegrityIndex -
                           (sanitationBacklog * 0.0450m) -
                           (pressure.FreezePressure * 0.0350m) -
                           (state.FloodingIndex.Value * 0.0400m) -
                           (pressure.StormPressure * 0.0200m) +
                           (currentSanitationInfrastructure.CrewReadinessIndex * 0.0400m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal sanitationOverflowControl = Smooth(
                current: currentSanitationInfrastructure.OverflowControlIndex,
                target: Clamp(
                    value: currentSanitationInfrastructure.OverflowControlIndex -
                           (pressure.RainPressure * 0.0500m) -
                           (pressure.StormPressure * 0.0450m) -
                           (state.FloodingIndex.Value * 0.0600m) -
                           (currentSanitationInfrastructure.IncidentPressureIndex * 0.0300m) +
                           (currentSanitationInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.0200m) +
                           (sanitationEmergencyModeBoost * 0.2000m)),
                factor: 0.18m,
                responseScale: responseScale);
            decimal sanitationCrewReadiness = Smooth(
                current: currentSanitationInfrastructure.CrewReadinessIndex,
                target: Clamp(
                    value: currentSanitationInfrastructure.CrewReadinessIndex +
                           (currentSanitationInfrastructure.EmergencyModeEnabled
                               ? -0.0500m
                               : 0.0260m) -
                           (currentSanitationInfrastructure.IncidentPressureIndex * 0.0300m) -
                           (sanitationBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal sanitationIncidentPressure = Smooth(
                current: currentSanitationInfrastructure.IncidentPressureIndex,
                target: Clamp(
                    value: currentSanitationInfrastructure.IncidentPressureIndex +
                           ((1m - sanitationCoverage) * 0.1200m) +
                           (sanitationFailureRisk * 0.0800m) +
                           (state.FloodingIndex.Value * 0.0800m) +
                           (pressure.StormPressure * 0.0500m) -
                           (sanitationCrewReadiness * 0.0600m) -
                           (sanitationOverflowControl * 0.0400m) -
                           (sanitationEmergencyModeBoost * 0.4000m)),
                factor: 0.26m,
                responseScale: responseScale);

            decimal powerLoad = Smooth(
                current: currentPowerDistribution.LoadIndex,
                target: Clamp(
                    value: (pressure.StormPressure * 0.36m) +
                           (pressure.FreezePressure * 0.20m) +
                           (state.FloodingIndex.Value * 0.14m) +
                           (state.SnowAccumulationIndex.Value * 0.08m) +
                           ((1m - state.PowerCoverageIndex.Value) * 0.22m) -
                           (currentPowerDistributionInfrastructure.SubstationCapacityIndex * 0.0800m) -
                           (currentPowerDistributionInfrastructure.GridIntegrityIndex * 0.0500m) -
                           (currentPowerDistributionInfrastructure.SwitchingReadinessIndex * 0.0500m) -
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.42m,
                responseScale: responseScale);
            decimal powerService = Smooth(
                current: currentPowerDistribution.ServiceQualityIndex,
                target: Clamp(
                    value: 0.64m +
                           (currentPowerDistributionInfrastructure.SubstationCapacityIndex * 0.0800m) +
                           (currentPowerDistributionInfrastructure.GridIntegrityIndex * 0.0600m) +
                           (currentPowerDistributionInfrastructure.SwitchingReadinessIndex * 0.0800m) -
                           (currentPowerDistribution.BacklogIndex * 0.20m) -
                           (pressure.StormPressure * 0.10m) -
                           (pressure.FreezePressure * 0.06m) +
                           (powerEmergencyModeBoost * 0.4200m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal powerBacklog = Smooth(
                current: currentPowerDistribution.BacklogIndex,
                target: Clamp(
                    value: currentPowerDistribution.BacklogIndex +
                           (powerLoad * 0.20m) -
                           (powerService * 0.16m) -
                           (pressure.ThawRelief * 0.03m) -
                           (currentPowerDistributionInfrastructure.CrewReadinessIndex * 0.0400m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal powerFailureRisk = Smooth(
                current: currentPowerDistribution.FailureRiskIndex,
                target: Clamp(
                    value: (powerLoad * 0.38m) +
                           (powerBacklog * 0.30m) +
                           ((1m - powerService) * 0.26m) +
                           (currentPowerDistributionInfrastructure.IncidentPressureIndex * 0.1400m) +
                           ((1m - currentPowerDistributionInfrastructure.GridIntegrityIndex) * 0.1000m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal powerCoverage = Smooth(
                current: state.PowerCoverageIndex.Value,
                target: Clamp(
                    value: 1.02m -
                           (pressure.StormPressure * 0.20m) -
                           (pressure.FreezePressure * 0.12m) -
                           (state.FloodingIndex.Value * 0.10m) -
                           (powerBacklog * 0.16m) -
                           (powerFailureRisk * 0.10m) +
                           (powerService * 0.12m) +
                           (currentPowerDistributionInfrastructure.SubstationCapacityIndex * 0.0800m) +
                           (currentPowerDistributionInfrastructure.SwitchingReadinessIndex * 0.0600m) +
                           (currentPowerDistributionInfrastructure.CrewReadinessIndex * 0.0400m) +
                           (pressure.ThawRelief * 0.02m) -
                           (currentPowerDistributionInfrastructure.IncidentPressureIndex * 0.0600m),
                    min: 0.10m,
                    max: 1m),
                factor: 0.46m,
                responseScale: responseScale);

            decimal powerSubstationCapacity = Smooth(
                current: currentPowerDistributionInfrastructure.SubstationCapacityIndex,
                target: Clamp(
                    value: currentPowerDistributionInfrastructure.SubstationCapacityIndex -
                           (powerLoad * 0.0500m) -
                           (pressure.StormPressure * 0.0400m) -
                           (currentPowerDistributionInfrastructure.IncidentPressureIndex * 0.0400m) +
                           (currentPowerDistributionInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (powerEmergencyModeBoost * 0.3000m)),
                factor: 0.20m,
                responseScale: responseScale);
            decimal powerGridIntegrity = Smooth(
                current: currentPowerDistributionInfrastructure.GridIntegrityIndex,
                target: Clamp(
                    value: currentPowerDistributionInfrastructure.GridIntegrityIndex -
                           (powerBacklog * 0.0450m) -
                           (pressure.StormPressure * 0.0500m) -
                           (state.FloodingIndex.Value * 0.0300m) -
                           (pressure.FreezePressure * 0.0250m) +
                           (currentPowerDistributionInfrastructure.CrewReadinessIndex * 0.0400m) +
                           (pressure.ThawRelief * 0.0150m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal powerSwitchingReadiness = Smooth(
                current: currentPowerDistributionInfrastructure.SwitchingReadinessIndex,
                target: Clamp(
                    value: currentPowerDistributionInfrastructure.SwitchingReadinessIndex -
                           (powerLoad * 0.0250m) -
                           (pressure.StormPressure * 0.0300m) -
                           (currentPowerDistributionInfrastructure.IncidentPressureIndex * 0.0300m) +
                           (currentPowerDistributionInfrastructure.CrewReadinessIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.0200m) +
                           (powerEmergencyModeBoost * 0.1800m)),
                factor: 0.18m,
                responseScale: responseScale);
            decimal powerCrewReadiness = Smooth(
                current: currentPowerDistributionInfrastructure.CrewReadinessIndex,
                target: Clamp(
                    value: currentPowerDistributionInfrastructure.CrewReadinessIndex +
                           (currentPowerDistributionInfrastructure.EmergencyModeEnabled
                               ? -0.0550m
                               : 0.0260m) -
                           (currentPowerDistributionInfrastructure.IncidentPressureIndex * 0.0300m) -
                           (powerBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal powerIncidentPressure = Smooth(
                current: currentPowerDistributionInfrastructure.IncidentPressureIndex,
                target: Clamp(
                    value: currentPowerDistributionInfrastructure.IncidentPressureIndex +
                           ((1m - powerCoverage) * 0.1300m) +
                           (powerFailureRisk * 0.0900m) +
                           (pressure.StormPressure * 0.0700m) +
                           (state.FloodingIndex.Value * 0.0600m) -
                           (powerCrewReadiness * 0.0600m) -
                           (powerSwitchingReadiness * 0.0400m) -
                           (powerEmergencyModeBoost * 0.4200m)),
                factor: 0.26m,
                responseScale: responseScale);

            decimal utilityIncidentLoad = Smooth(
                current: currentUtilityIncidents.LoadIndex,
                target: Clamp(
                    value: ((1m - powerCoverage) * 0.22m) +
                           ((1m - heatingCoverage) * 0.16m) +
                           ((1m - waterCoverage) * 0.18m) +
                           ((1m - sanitationCoverage) * 0.16m) +
                           (pressure.StormPressure * 0.12m) +
                           (state.FloodingIndex.Value * 0.10m) +
                           (pressure.FreezePressure * 0.08m) +
                           (powerFailureRisk * 0.06m) +
                           (heatingFailureRisk * 0.05m) +
                           (waterFailureRisk * 0.06m) +
                           (sanitationFailureRisk * 0.05m) -
                           (pressure.UtilityIncidentSupport * 0.18m) -
                           (currentUtilityIncidentInfrastructure.RestorationCoverageIndex * 0.0600m) -
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.42m,
                responseScale: responseScale);
            decimal utilityIncidentService = Smooth(
                current: currentUtilityIncidents.ServiceQualityIndex,
                target: Clamp(
                    value: 0.60m +
                           (pressure.UtilityIncidentSupport * 0.30m) +
                           (currentUtilityIncidentInfrastructure.DispatchReadinessIndex * 0.0800m) +
                           (currentUtilityIncidentInfrastructure.FieldCoordinationIndex * 0.0500m) -
                           (currentUtilityIncidents.BacklogIndex * 0.18m) -
                           (currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.0800m) +
                           (utilityIncidentEmergencyModeBoost * 0.4200m),
                    min: 0.05m,
                    max: 1m),
                factor: 0.35m,
                responseScale: responseScale);
            decimal utilityIncidentBacklog = Smooth(
                current: currentUtilityIncidents.BacklogIndex,
                target: Clamp(
                    value: currentUtilityIncidents.BacklogIndex +
                           (utilityIncidentLoad * 0.18m) -
                           (utilityIncidentService * 0.15m) -
                           (pressure.ThawRelief * 0.03m) -
                           (currentUtilityIncidentInfrastructure.DispatchReadinessIndex * 0.0300m)),
                factor: 0.40m,
                responseScale: responseScale);
            decimal utilityIncidentFailureRisk = Smooth(
                current: currentUtilityIncidents.FailureRiskIndex,
                target: Clamp(
                    value: (utilityIncidentLoad * 0.36m) +
                           (utilityIncidentBacklog * 0.30m) +
                           ((1m - utilityIncidentService) * 0.24m) +
                           (currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.1400m)),
                factor: 0.30m,
                responseScale: responseScale);

            decimal utilityContinuity = Smooth(
                current: state.UtilityContinuityIndex.Value,
                target: Clamp(
                    value: 1.02m -
                           ((1m - powerCoverage) * 0.22m) -
                           ((1m - heatingCoverage) * 0.16m) -
                           ((1m - waterCoverage) * 0.18m) -
                           ((1m - sanitationCoverage) * 0.18m) -
                           (utilityIncidentBacklog * 0.12m) -
                           (utilityIncidentFailureRisk * 0.08m) +
                           (utilityIncidentService * 0.10m) +
                           (pressure.UtilityIncidentSupport * 0.08m) +
                           (currentUtilityIncidentInfrastructure.RestorationCoverageIndex * 0.0600m) +
                           (currentUtilityIncidentInfrastructure.FieldCoordinationIndex * 0.0400m) -
                           (currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.0500m),
                    min: 0.10m,
                    max: 1m),
                factor: 0.46m,
                responseScale: responseScale);

            decimal utilityIncidentDispatchReadiness = Smooth(
                current: currentUtilityIncidentInfrastructure.DispatchReadinessIndex,
                target: Clamp(
                    value: currentUtilityIncidentInfrastructure.DispatchReadinessIndex -
                           (currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.0300m) -
                           (utilityIncidentLoad * 0.0200m) +
                           (currentUtilityIncidentInfrastructure.FieldCoordinationIndex * 0.0500m) +
                           (pressure.ThawRelief * 0.0150m) +
                           (utilityIncidentEmergencyModeBoost * 0.1800m)),
                factor: 0.18m,
                responseScale: responseScale);
            decimal utilityIncidentRestorationCoverage = Smooth(
                current: currentUtilityIncidentInfrastructure.RestorationCoverageIndex,
                target: Clamp(
                    value: currentUtilityIncidentInfrastructure.RestorationCoverageIndex -
                           ((1m - powerCoverage) * 0.0600m) -
                           ((1m - heatingCoverage) * 0.0400m) -
                           ((1m - waterCoverage) * 0.0500m) -
                           ((1m - sanitationCoverage) * 0.0500m) -
                           (utilityIncidentBacklog * 0.0500m) +
                           (utilityIncidentDispatchReadiness * 0.0600m) +
                           (currentUtilityIncidentInfrastructure.FieldCoordinationIndex * 0.0500m) +
                           (utilityIncidentEmergencyModeBoost * 0.2500m)),
                factor: 0.22m,
                responseScale: responseScale);
            decimal utilityIncidentSpareCapacity = Smooth(
                current: currentUtilityIncidentInfrastructure.SpareCapacityIndex,
                target: Clamp(
                    value: currentUtilityIncidentInfrastructure.SpareCapacityIndex -
                           (utilityIncidentLoad * 0.0400m) -
                           (pressure.StormPressure * 0.0300m) -
                           (currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.0300m) +
                           (currentUtilityIncidentInfrastructure.FieldCoordinationIndex * 0.0400m) +
                           (pressure.ThawRelief * 0.0150m) +
                           (utilityIncidentEmergencyModeBoost * 0.1800m)),
                factor: 0.18m,
                responseScale: responseScale);
            decimal utilityIncidentFieldCoordination = Smooth(
                current: currentUtilityIncidentInfrastructure.FieldCoordinationIndex,
                target: Clamp(
                    value: currentUtilityIncidentInfrastructure.FieldCoordinationIndex +
                           (currentUtilityIncidentInfrastructure.EmergencyModeEnabled
                               ? -0.0500m
                               : 0.0260m) -
                           (currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex * 0.0300m) -
                           (utilityIncidentBacklog * 0.0200m) +
                           (pressure.ThawRelief * 0.0200m)),
                factor: 0.16m,
                responseScale: responseScale);
            decimal utilityIncidentQueuePressure = Smooth(
                current: currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex,
                target: Clamp(
                    value: currentUtilityIncidentInfrastructure.IncidentQueuePressureIndex +
                           (utilityIncidentLoad * 0.1400m) +
                           (utilityIncidentFailureRisk * 0.0800m) +
                           ((1m - powerCoverage) * 0.0600m) +
                           ((1m - waterCoverage) * 0.0500m) -
                           (utilityIncidentFieldCoordination * 0.0600m) -
                           (utilityIncidentRestorationCoverage * 0.0400m) -
                           (utilityIncidentEmergencyModeBoost * 0.4000m)),
                factor: 0.26m,
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
                snowRemovalInfrastructure: new CitySnowRemovalInfrastructureSnapshot(
                    fleetAvailabilityIndex: snowFleetAvailability,
                    routeCoverageIndex: snowRouteCoverage,
                    deicingReadinessIndex: snowDeicingReadiness,
                    crewReadinessIndex: snowCrewReadiness,
                    incidentPressureIndex: snowIncidentPressure,
                    emergencyModeEnabled: currentSnowRemovalInfrastructure.EmergencyModeEnabled),
                roadAccess: new CitySystemSnapshot(
                    kind: CitySystemKind.RoadAccess,
                    loadIndex: roadLoad,
                    serviceQualityIndex: roadService,
                    backlogIndex: roadBacklog,
                    failureRiskIndex: roadFailureRisk),
                roadAccessInfrastructure: new CityRoadAccessInfrastructureSnapshot(
                    corridorAvailabilityIndex: roadCorridorAvailability,
                    surfaceIntegrityIndex: roadSurfaceIntegrity,
                    trafficControlReadinessIndex: roadTrafficControlReadiness,
                    crewReadinessIndex: roadCrewReadiness,
                    incidentPressureIndex: roadIncidentPressure,
                    emergencyModeEnabled: currentRoadAccessInfrastructure.EmergencyModeEnabled),
                heating: new CitySystemSnapshot(
                    kind: CitySystemKind.Heating,
                    loadIndex: heatingLoad,
                    serviceQualityIndex: heatingService,
                    backlogIndex: heatingBacklog,
                    failureRiskIndex: heatingFailureRisk),
                heatingInfrastructure: new CityHeatingInfrastructureSnapshot(
                    plantCapacityIndex: heatingPlantCapacity,
                    networkIntegrityIndex: heatingNetworkIntegrity,
                    controlReadinessIndex: heatingControlReadiness,
                    crewReadinessIndex: heatingCrewReadiness,
                    incidentPressureIndex: heatingIncidentPressure,
                    emergencyModeEnabled: currentHeatingInfrastructure.EmergencyModeEnabled),
                waterDistribution: new CitySystemSnapshot(
                    kind: CitySystemKind.WaterDistribution,
                    loadIndex: waterLoad,
                    serviceQualityIndex: waterService,
                    backlogIndex: waterBacklog,
                    failureRiskIndex: waterFailureRisk),
                waterDistributionInfrastructure: new CityWaterDistributionInfrastructureSnapshot(
                    treatmentCapacityIndex: waterTreatmentCapacity,
                    networkIntegrityIndex: waterNetworkIntegrity,
                    pumpReadinessIndex: waterPumpReadiness,
                    crewReadinessIndex: waterCrewReadiness,
                    incidentPressureIndex: waterIncidentPressure,
                    emergencyModeEnabled: currentWaterDistributionInfrastructure.EmergencyModeEnabled),
                sanitation: new CitySystemSnapshot(
                    kind: CitySystemKind.Sanitation,
                    loadIndex: sanitationLoad,
                    serviceQualityIndex: sanitationService,
                    backlogIndex: sanitationBacklog,
                    failureRiskIndex: sanitationFailureRisk),
                sanitationInfrastructure: new CitySanitationInfrastructureSnapshot(
                    treatmentStabilityIndex: sanitationTreatmentStability,
                    networkIntegrityIndex: sanitationNetworkIntegrity,
                    overflowControlIndex: sanitationOverflowControl,
                    crewReadinessIndex: sanitationCrewReadiness,
                    incidentPressureIndex: sanitationIncidentPressure,
                    emergencyModeEnabled: currentSanitationInfrastructure.EmergencyModeEnabled),
                powerDistribution: new CitySystemSnapshot(
                    kind: CitySystemKind.PowerDistribution,
                    loadIndex: powerLoad,
                    serviceQualityIndex: powerService,
                    backlogIndex: powerBacklog,
                    failureRiskIndex: powerFailureRisk),
                powerDistributionInfrastructure: new CityPowerDistributionInfrastructureSnapshot(
                    substationCapacityIndex: powerSubstationCapacity,
                    gridIntegrityIndex: powerGridIntegrity,
                    switchingReadinessIndex: powerSwitchingReadiness,
                    crewReadinessIndex: powerCrewReadiness,
                    incidentPressureIndex: powerIncidentPressure,
                    emergencyModeEnabled: currentPowerDistributionInfrastructure.EmergencyModeEnabled),
                utilityIncidents: new CitySystemSnapshot(
                    kind: CitySystemKind.UtilityIncidents,
                    loadIndex: utilityIncidentLoad,
                    serviceQualityIndex: utilityIncidentService,
                    backlogIndex: utilityIncidentBacklog,
                    failureRiskIndex: utilityIncidentFailureRisk),
                utilityIncidentInfrastructure: new CityUtilityIncidentInfrastructureSnapshot(
                    dispatchReadinessIndex: utilityIncidentDispatchReadiness,
                    restorationCoverageIndex: utilityIncidentRestorationCoverage,
                    spareCapacityIndex: utilityIncidentSpareCapacity,
                    fieldCoordinationIndex: utilityIncidentFieldCoordination,
                    incidentQueuePressureIndex: utilityIncidentQueuePressure,
                    emergencyModeEnabled: currentUtilityIncidentInfrastructure.EmergencyModeEnabled),
                floodingIndex: FloodingIndex.From(flooding),
                snowAccumulationIndex: SnowAccumulationIndex.From(snowAccumulation),
                roadAccessibilityIndex: RoadAccessibilityIndex.From(roadAccessibility),
                heatingCoverageIndex: HeatingCoverageIndex.From(heatingCoverage),
                waterCoverageIndex: WaterCoverageIndex.From(waterCoverage),
                sanitationCoverageIndex: SanitationCoverageIndex.From(sanitationCoverage),
                powerCoverageIndex: PowerCoverageIndex.From(powerCoverage),
                utilityContinuityIndex: UtilityContinuityIndex.From(utilityContinuity),
                resourceSupply: state.ResourceSupply.ToSnapshot(),
                operationalBudgetPressure: state.OperationalBudgetPressure.ToSnapshot(),
                evaluatedAtUtc: asOfUtc);
        }

        private static decimal GetDevelopmentSeverity(string developmentLevel)
        {
            return developmentLevel.Trim()
                   .ToLowerInvariant() switch
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
            byte[] hash = SHA256.HashData(source: Encoding.UTF8.GetBytes($"{cityId:N}|{salt}"));
            int sample = BitConverter.ToInt32(
                             value: hash,
                             startIndex: 0) &
                         int.MaxValue;

            decimal normalized = sample / (decimal)int.MaxValue;
            decimal centered = (normalized - 0.5m) * 2m;

            return Clamp(value: baseline + (centered * maxAbsJitter));
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

            double scaledFactor = 1d -
                                  Math.Pow(
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
