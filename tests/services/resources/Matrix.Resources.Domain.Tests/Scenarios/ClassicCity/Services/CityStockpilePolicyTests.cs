using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Domain.Tests.TestSupport.ResourcesTestData;

namespace Matrix.Resources.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityStockpilePolicyTests
    {
        [Fact]
        public void CreateSeed_RespectsDevelopmentProfiles()
        {
            var policy = new CityStockpilePolicy();

            CityStockpileSnapshot struggling = policy.CreateSeed(
                developmentLevel: "struggling",
                createdAtUtc: CreatedAtUtc);
            CityStockpileSnapshot affluent = policy.CreateSeed(
                developmentLevel: "affluent",
                createdAtUtc: CreatedAtUtc);

            Assert.True(affluent.Food.StockLevelIndex > struggling.Food.StockLevelIndex);
            Assert.True(affluent.Fuel.ResupplyReadinessIndex > struggling.Fuel.ResupplyReadinessIndex);
            Assert.True(struggling.SupplyStressIndex > affluent.SupplyStressIndex);
        }

        [Fact]
        public void Advance_WithNonPositiveElapsed_ReturnsSameSnapshot()
        {
            var policy = new CityStockpilePolicy();
            CityStockpileSnapshot current = CreateSnapshot();

            CityStockpileSnapshot advanced = policy.Advance(
                current: current,
                elapsed: TimeSpan.Zero);

            Assert.Same(
                expected: current,
                actual: advanced);
        }

        [Fact]
        public void Advance_WithElapsedTime_UpdatesStockAndEvaluationTime()
        {
            var policy = new CityStockpilePolicy();
            CityStockpileSnapshot current = CreateSnapshot();

            CityStockpileSnapshot advanced = policy.Advance(
                current: current,
                elapsed: TimeSpan.FromDays(1));

            Assert.Equal(
                expected: CreatedAtUtc.AddDays(1),
                actual: advanced.EvaluatedAtUtc);
            Assert.NotEqual(
                expected: current.Fuel.StockLevelIndex,
                actual: advanced.Fuel.StockLevelIndex);
            Assert.NotEqual(
                expected: current.SupplyStressIndex,
                actual: advanced.SupplyStressIndex);
        }

        [Fact]
        public void SetEmergencyRationing_UpdatesFlagAndStress()
        {
            var policy = new CityStockpilePolicy();
            CityStockpileSnapshot current = CreateSnapshot(
                emergencyRationingEnabled: false,
                supplyStressIndex: 0.40m);

            CityStockpileSnapshot updated = policy.SetEmergencyRationing(
                current: current,
                enabled: true);

            Assert.True(updated.EmergencyRationingEnabled);
            Assert.True(updated.SupplyStressIndex < current.SupplyStressIndex);
        }

        [Fact]
        public void DispatchResupply_FocusedResourceImprovesMoreThanUnfocused()
        {
            var policy = new CityStockpilePolicy();
            CityStockpileSnapshot current = CreateSnapshot();

            CityStockpileSnapshot updated = policy.DispatchResupply(
                current: current,
                focus: ResupplyFocus.Medicine,
                intensity: ResupplyIntensity.High);

            decimal medicineGain = updated.Medicine.StockLevelIndex - current.Medicine.StockLevelIndex;
            decimal foodGain = updated.Food.StockLevelIndex - current.Food.StockLevelIndex;

            Assert.True(medicineGain > foodGain);
            Assert.True(updated.Medicine.ShortageRiskIndex < current.Medicine.ShortageRiskIndex);
        }

        [Fact]
        public void ApplySystemsDemand_FutureDemandIsNoOp_AndCurrentDemandMutatesOperationalLines()
        {
            var policy = new CityStockpilePolicy();
            CityStockpileSnapshot futureDemand = CreateSnapshot(evaluatedAtUtc: CreatedAtUtc);
            futureDemand = futureDemand with
            {
                SystemsDemand = CreateSystemsDemand(
                    fuelDemandPressureIndex: 0.70m,
                    sparePartsDemandPressureIndex: 0.60m,
                    filtersDemandPressureIndex: 0.50m,
                    emergencyWaterDemandPressureIndex: 0.40m,
                    effectiveAtUtc: CreatedAtUtc.AddHours(2))
            };

            CityStockpileSnapshot noOp = policy.ApplySystemsDemand(futureDemand);

            Assert.Same(
                expected: futureDemand,
                actual: noOp);

            CityStockpileSnapshot activeDemand = futureDemand with
            {
                SystemsDemand = CreateSystemsDemand(
                    fuelDemandPressureIndex: 0.70m,
                    sparePartsDemandPressureIndex: 0.60m,
                    filtersDemandPressureIndex: 0.50m,
                    emergencyWaterDemandPressureIndex: 0.40m,
                    effectiveAtUtc: CreatedAtUtc.AddHours(-1))
            };

            CityStockpileSnapshot applied = policy.ApplySystemsDemand(activeDemand);

            Assert.True(applied.Fuel.DemandPressureIndex > activeDemand.Fuel.DemandPressureIndex);
            Assert.True(applied.SpareParts.DemandPressureIndex > activeDemand.SpareParts.DemandPressureIndex);
            Assert.Equal(
                expected: activeDemand.Food,
                actual: applied.Food);
        }
    }
}
