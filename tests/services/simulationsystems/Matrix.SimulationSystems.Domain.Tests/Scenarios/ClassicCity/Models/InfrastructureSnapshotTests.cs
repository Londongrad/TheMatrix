using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Models
{
    public sealed class InfrastructureSnapshotTests
    {
        [Fact]
        public void InfrastructureSnapshots_RoundNormalizedMetrics()
        {
            var drainage = new CityDrainageInfrastructureSnapshot(
                pumpCapacityIndex: 0.12345m,
                networkIntegrityIndex: 0.23455m,
                blockageIndex: 0.34565m,
                crewReadinessIndex: 0.45675m,
                incidentPressureIndex: 0.56785m,
                emergencyModeEnabled: false);
            var road = new CityRoadAccessInfrastructureSnapshot(
                corridorAvailabilityIndex: 0.11115m,
                surfaceIntegrityIndex: 0.22225m,
                trafficControlReadinessIndex: 0.33335m,
                crewReadinessIndex: 0.44445m,
                incidentPressureIndex: 0.55555m,
                emergencyModeEnabled: false);
            var water = new CityWaterDistributionInfrastructureSnapshot(
                treatmentCapacityIndex: 0.66665m,
                networkIntegrityIndex: 0.77775m,
                pumpReadinessIndex: 0.88885m,
                crewReadinessIndex: 0.99995m,
                incidentPressureIndex: 0.10105m,
                emergencyModeEnabled: false);

            Assert.Equal(
                expected: 0.1235m,
                actual: drainage.PumpCapacityIndex);
            Assert.Equal(
                expected: 0.2346m,
                actual: drainage.NetworkIntegrityIndex);
            Assert.Equal(
                expected: 0.3334m,
                actual: road.TrafficControlReadinessIndex);
            Assert.Equal(
                expected: 1m,
                actual: water.CrewReadinessIndex);
            Assert.Equal(
                expected: 0.1011m,
                actual: water.IncidentPressureIndex);
        }

        [Fact]
        public void InfrastructureSnapshots_WhenValueIsOutsideRange_Throw()
        {
            Assert.ThrowsAny<Exception>(() => new CityHeatingInfrastructureSnapshot(
                plantCapacityIndex: -0.01m,
                networkIntegrityIndex: 0.4m,
                controlReadinessIndex: 0.5m,
                crewReadinessIndex: 0.6m,
                incidentPressureIndex: 0.7m,
                emergencyModeEnabled: false));
            Assert.ThrowsAny<Exception>(() => new CitySanitationInfrastructureSnapshot(
                treatmentStabilityIndex: 0.4m,
                networkIntegrityIndex: 1.01m,
                overflowControlIndex: 0.5m,
                crewReadinessIndex: 0.6m,
                incidentPressureIndex: 0.7m,
                emergencyModeEnabled: false));
            Assert.ThrowsAny<Exception>(() => new CityPowerDistributionInfrastructureSnapshot(
                substationCapacityIndex: 0.4m,
                gridIntegrityIndex: 0.5m,
                switchingReadinessIndex: 1.01m,
                crewReadinessIndex: 0.6m,
                incidentPressureIndex: 0.7m,
                emergencyModeEnabled: false));
            Assert.ThrowsAny<Exception>(() => new CitySnowRemovalInfrastructureSnapshot(
                fleetAvailabilityIndex: 0.4m,
                routeCoverageIndex: 0.5m,
                deicingReadinessIndex: 0.6m,
                crewReadinessIndex: -0.01m,
                incidentPressureIndex: 0.7m,
                emergencyModeEnabled: false));
            Assert.ThrowsAny<Exception>(() => new CityUtilityIncidentInfrastructureSnapshot(
                dispatchReadinessIndex: 0.4m,
                restorationCoverageIndex: 0.5m,
                spareCapacityIndex: 0.6m,
                fieldCoordinationIndex: 0.7m,
                incidentQueuePressureIndex: 1.01m,
                emergencyModeEnabled: false));
        }

        [Fact]
        public void CitySystemPressureProfile_RoundsAndValidatesInputs()
        {
            var profile = new CitySystemPressureProfile(
                rainPressure: 0.12345m,
                snowPressure: 0.22345m,
                stormPressure: 0.32345m,
                freezePressure: 0.42345m,
                thawRelief: 0.52345m,
                drainageSupport: 0.62345m,
                snowRemovalSupport: 0.72345m,
                roadSupport: 0.82345m,
                powerSupport: 0.92345m,
                utilityIncidentSupport: 0.13335m,
                heatingSupport: 0.23335m,
                waterSupport: 0.33335m,
                sanitationSupport: 0.43335m);

            Assert.Equal(
                expected: 0.1235m,
                actual: profile.RainPressure);
            Assert.Equal(
                expected: 0.9235m,
                actual: profile.PowerSupport);
            Assert.Equal(
                expected: 0.4334m,
                actual: profile.SanitationSupport);

            Assert.ThrowsAny<Exception>(() => new CitySystemPressureProfile(
                rainPressure: 1.01m,
                snowPressure: 0m,
                stormPressure: 0m,
                freezePressure: 0m,
                thawRelief: 0m,
                drainageSupport: 0m,
                snowRemovalSupport: 0m,
                roadSupport: 0m));
        }
    }
}
