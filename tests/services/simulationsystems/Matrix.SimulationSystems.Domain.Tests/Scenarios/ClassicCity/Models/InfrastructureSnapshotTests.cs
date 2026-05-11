using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Models;

public sealed class InfrastructureSnapshotTests
{
    [Fact]
    public void InfrastructureSnapshots_RoundNormalizedMetrics()
    {
        var drainage = new CityDrainageInfrastructureSnapshot(0.12345m, 0.23455m, 0.34565m, 0.45675m, 0.56785m, false);
        var road = new CityRoadAccessInfrastructureSnapshot(0.11115m, 0.22225m, 0.33335m, 0.44445m, 0.55555m, false);
        var water = new CityWaterDistributionInfrastructureSnapshot(0.66665m, 0.77775m, 0.88885m, 0.99995m, 0.10105m, false);

        Assert.Equal(0.1235m, drainage.PumpCapacityIndex);
        Assert.Equal(0.2346m, drainage.NetworkIntegrityIndex);
        Assert.Equal(0.3334m, road.TrafficControlReadinessIndex);
        Assert.Equal(1m, water.CrewReadinessIndex);
        Assert.Equal(0.1011m, water.IncidentPressureIndex);
    }

    [Fact]
    public void InfrastructureSnapshots_WhenValueIsOutsideRange_Throw()
    {
        Assert.ThrowsAny<Exception>(() => new CityHeatingInfrastructureSnapshot(-0.01m, 0.4m, 0.5m, 0.6m, 0.7m, false));
        Assert.ThrowsAny<Exception>(() => new CitySanitationInfrastructureSnapshot(0.4m, 1.01m, 0.5m, 0.6m, 0.7m, false));
        Assert.ThrowsAny<Exception>(() => new CityPowerDistributionInfrastructureSnapshot(0.4m, 0.5m, 1.01m, 0.6m, 0.7m, false));
        Assert.ThrowsAny<Exception>(() => new CitySnowRemovalInfrastructureSnapshot(0.4m, 0.5m, 0.6m, -0.01m, 0.7m, false));
        Assert.ThrowsAny<Exception>(() => new CityUtilityIncidentInfrastructureSnapshot(0.4m, 0.5m, 0.6m, 0.7m, 1.01m, false));
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

        Assert.Equal(0.1235m, profile.RainPressure);
        Assert.Equal(0.9235m, profile.PowerSupport);
        Assert.Equal(0.4334m, profile.SanitationSupport);

        Assert.ThrowsAny<Exception>(
            () => new CitySystemPressureProfile(
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
