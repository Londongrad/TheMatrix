using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Models
{
    public sealed class EnvironmentalModelTests
    {
        [Fact]
        public void CitySystemSnapshot_WhenKindIsInvalid_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new CitySystemSnapshot(
                kind: (CitySystemKind)999,
                loadIndex: 0.1m,
                serviceQualityIndex: 0.9m,
                backlogIndex: 0.1m,
                failureRiskIndex: 0.1m));
        }

        [Fact]
        public void CitySystemSnapshot_RoundsNormalizedMetrics()
        {
            var snapshot = new CitySystemSnapshot(
                kind: CitySystemKind.Drainage,
                loadIndex: 0.12345m,
                serviceQualityIndex: 0.87654m,
                backlogIndex: 0.22225m,
                failureRiskIndex: 0.33335m);

            Assert.Equal(
                expected: 0.1235m,
                actual: snapshot.LoadIndex);
            Assert.Equal(
                expected: 0.8765m,
                actual: snapshot.ServiceQualityIndex);
            Assert.Equal(
                expected: 0.2223m,
                actual: snapshot.BacklogIndex);
            Assert.Equal(
                expected: 0.3334m,
                actual: snapshot.FailureRiskIndex);
        }

        [Fact]
        public void CityWeatherPressureProfile_NeutralCreatesZeroedProfile()
        {
            var profile = CityWeatherPressureProfile.Neutral();

            Assert.Equal(
                expected: 0m,
                actual: profile.RainPressure);
            Assert.Equal(
                expected: 0m,
                actual: profile.SnowPressure);
            Assert.Equal(
                expected: 0m,
                actual: profile.StormPressure);
            Assert.Equal(
                expected: 0m,
                actual: profile.FreezePressure);
            Assert.Equal(
                expected: 0m,
                actual: profile.ThawRelief);
        }

        [Fact]
        public void CityResourceSupplySnapshot_NeutralCreatesStableDefaults()
        {
            var snapshot = CityResourceSupplySnapshot.Neutral(
                effectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc,
                effectiveTickId: 3);

            Assert.Equal(
                expected: 0m,
                actual: snapshot.SupplyStressIndex);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.FuelStockLevelIndex);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.EmergencyWaterResupplyReadinessIndex);
            Assert.Equal(
                expected: 3,
                actual: snapshot.EffectiveTickId);
            Assert.Equal(
                expected: SimulationSystemsTestData.CreatedAtUtc,
                actual: snapshot.EffectiveAtUtc);
        }

        [Fact]
        public void CityResourceSupplySnapshot_WhenTimestampIsNotUtc_Throws()
        {
            Assert.ThrowsAny<Exception>(() => CityResourceSupplySnapshot.Neutral(
                effectiveAtUtc: new DateTimeOffset(
                    year: 2051,
                    month: 2,
                    day: 3,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3))));
        }

        [Fact]
        public void CityOperationalBudgetPressureSnapshot_NeutralNormalizesTickAndUtc()
        {
            var snapshot = CityOperationalBudgetPressureSnapshot.Neutral(
                effectiveAtUtc: new DateTimeOffset(
                    year: 2051,
                    month: 2,
                    day: 3,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3)),
                effectiveTickId: -4);

            Assert.Equal(
                expected: 0m,
                actual: snapshot.PressureIndex);
            Assert.Equal(
                expected: 0,
                actual: snapshot.EffectiveTickId);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: snapshot.EffectiveAtUtc.Offset);
        }
    }
}
