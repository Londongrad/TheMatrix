using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationDistrictImpactPolicy
    {
        public CityPopulationLivingConditionsContext ResolveLivingConditions(
            DistrictId? districtId,
            CityPopulationLivingConditionsState? livingConditionsState,
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions = null)
        {
            CityPopulationLivingConditionsContext baseline = new(
                FloodingIndex: livingConditionsState?.FloodingIndex ?? 0m,
                RoadAccessibilityIndex: livingConditionsState?.RoadAccessibilityIndex ?? 1m,
                PowerCoverageIndex: livingConditionsState?.PowerCoverageIndex ?? 1m,
                UtilityContinuityIndex: livingConditionsState?.UtilityContinuityIndex ?? 1m,
                HeatingCoverageIndex: livingConditionsState?.HeatingCoverageIndex ?? 1m,
                WaterCoverageIndex: livingConditionsState?.WaterCoverageIndex ?? 1m,
                SanitationCoverageIndex: livingConditionsState?.SanitationCoverageIndex ?? 1m);

            if (!districtId.HasValue)
                return baseline;

            DistrictImpactProfile profile = BuildProfile(districtId.Value);

            decimal powerCoverageIndex = districtUtilityConditions?.PowerCoverageIndex ?? AdjustCoverage(
                value: baseline.PowerCoverageIndex,
                factor: profile.UtilityFragilityFactor);
            decimal heatingCoverageIndex = districtUtilityConditions?.HeatingCoverageIndex ?? AdjustCoverage(
                value: baseline.HeatingCoverageIndex,
                factor: profile.UtilityFragilityFactor * 0.94d);
            decimal waterCoverageIndex = districtUtilityConditions?.WaterCoverageIndex ?? AdjustCoverage(
                value: baseline.WaterCoverageIndex,
                factor: profile.UtilityFragilityFactor * 0.90d);
            decimal sanitationCoverageIndex = districtUtilityConditions?.SanitationCoverageIndex ?? AdjustCoverage(
                value: baseline.SanitationCoverageIndex,
                factor: profile.SanitationFragilityFactor);
            decimal utilityContinuityIndex = districtUtilityConditions is null
                ? AdjustCoverage(
                    value: baseline.UtilityContinuityIndex,
                    factor: profile.UtilityFragilityFactor * 0.96d)
                : ResolveUtilityContinuityIndex(
                    snapshot: districtUtilityConditions,
                    baselineUtilityContinuityIndex: baseline.UtilityContinuityIndex);

            return new CityPopulationLivingConditionsContext(
                FloodingIndex: AdjustPressure(
                    value: baseline.FloodingIndex,
                    factor: profile.FloodExposureFactor),
                RoadAccessibilityIndex: AdjustCoverage(
                    value: baseline.RoadAccessibilityIndex,
                    factor: profile.MobilityFragilityFactor * (1d + ((double)baseline.FloodingIndex * 0.12d))),
                PowerCoverageIndex: powerCoverageIndex,
                UtilityContinuityIndex: utilityContinuityIndex,
                HeatingCoverageIndex: heatingCoverageIndex,
                WaterCoverageIndex: waterCoverageIndex,
                SanitationCoverageIndex: sanitationCoverageIndex);
        }

        public CityPopulationEssentialsContext ResolveEssentials(
            DistrictId? districtId,
            CityPopulationEssentialsState? essentialsState)
        {
            CityPopulationEssentialsContext baseline = new(
                SupplyStressIndex: essentialsState?.SupplyStressIndex ?? 0m,
                EmergencyRationingEnabled: essentialsState?.EmergencyRationingEnabled == true,
                FoodStockLevelIndex: essentialsState?.FoodStockLevelIndex ?? 1m,
                FoodShortageRiskIndex: essentialsState?.FoodShortageRiskIndex ?? 0m,
                MedicineStockLevelIndex: essentialsState?.MedicineStockLevelIndex ?? 1m,
                MedicineShortageRiskIndex: essentialsState?.MedicineShortageRiskIndex ?? 0m,
                EmergencyWaterStockLevelIndex: essentialsState?.EmergencyWaterStockLevelIndex ?? 1m,
                EmergencyWaterShortageRiskIndex: essentialsState?.EmergencyWaterShortageRiskIndex ?? 0m);

            if (!districtId.HasValue)
                return baseline;

            DistrictImpactProfile profile = BuildProfile(districtId.Value);

            return new CityPopulationEssentialsContext(
                SupplyStressIndex: AdjustPressure(
                    value: baseline.SupplyStressIndex,
                    factor: profile.SupplyRouteFragilityFactor),
                EmergencyRationingEnabled: baseline.EmergencyRationingEnabled,
                FoodStockLevelIndex: AdjustCoverage(
                    value: baseline.FoodStockLevelIndex,
                    factor: profile.SupplyRouteFragilityFactor * 0.92d),
                FoodShortageRiskIndex: AdjustPressure(
                    value: baseline.FoodShortageRiskIndex,
                    factor: profile.SupplyRouteFragilityFactor),
                MedicineStockLevelIndex: AdjustCoverage(
                    value: baseline.MedicineStockLevelIndex,
                    factor: profile.SupplyRouteFragilityFactor * 0.88d),
                MedicineShortageRiskIndex: AdjustPressure(
                    value: baseline.MedicineShortageRiskIndex,
                    factor: profile.SupplyRouteFragilityFactor * 1.05d),
                EmergencyWaterStockLevelIndex: AdjustCoverage(
                    value: baseline.EmergencyWaterStockLevelIndex,
                    factor: profile.SupplyRouteFragilityFactor * 0.90d),
                EmergencyWaterShortageRiskIndex: AdjustPressure(
                    value: baseline.EmergencyWaterShortageRiskIndex,
                    factor: profile.SupplyRouteFragilityFactor));
        }

        private static DistrictImpactProfile BuildProfile(DistrictId districtId)
        {
            return new DistrictImpactProfile(
                FloodExposureFactor: 0.82d + (ResolveStableFraction(districtId, 11) * 0.52d),
                MobilityFragilityFactor: 0.84d + (ResolveStableFraction(districtId, 23) * 0.44d),
                UtilityFragilityFactor: 0.86d + (ResolveStableFraction(districtId, 37) * 0.38d),
                SanitationFragilityFactor: 0.88d + (ResolveStableFraction(districtId, 53) * 0.34d),
                SupplyRouteFragilityFactor: 0.85d + (ResolveStableFraction(districtId, 71) * 0.42d));
        }

        private static decimal AdjustCoverage(decimal value, double factor)
        {
            decimal normalized = Math.Clamp(value, 0m, 1.50m);
            double deficit = Math.Clamp((double)(1m - normalized), 0d, 1.50d);
            double adjustedDeficit = Math.Clamp(deficit * factor, 0d, 1.50d);

            return Round(Math.Clamp(1d - adjustedDeficit, 0d, 1.50d));
        }

        private static decimal AdjustPressure(decimal value, double factor)
        {
            return Round(Math.Clamp((double)value * factor, 0d, 1.50d));
        }

        private static decimal Round(double value)
        {
            return decimal.Round(
                d: (decimal)value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveUtilityContinuityIndex(
            CityDistrictUtilityConditionsSnapshot snapshot,
            decimal baselineUtilityContinuityIndex)
        {
            decimal continuity = (snapshot.PowerCoverageIndex * 0.34m) +
                                 (snapshot.HeatingCoverageIndex * 0.18m) +
                                 (snapshot.WaterCoverageIndex * 0.20m) +
                                 (snapshot.SanitationCoverageIndex * 0.16m) +
                                 ((1m - snapshot.PowerOutageRiskIndex) * 0.05m) +
                                 ((1m - snapshot.WaterDisruptionRiskIndex) * 0.04m) +
                                 ((1m - snapshot.SanitationContaminationRiskIndex) * 0.03m);
            decimal incidentAdjustment = (snapshot.UtilityIncidentDispatchReadinessIndex * 0.12m) -
                                         (snapshot.UtilityIncidentPressureIndex * 0.16m) -
                                         (snapshot.UtilityIncidentCoordinationDifficultyIndex * 0.08m) -
                                         (snapshot.UtilityIncidentRestorationPriorityIndex * 0.06m);
            decimal adjustedContinuity = Math.Clamp(
                value: continuity + incidentAdjustment,
                min: 0m,
                max: 1.5m);
            decimal blended = (baselineUtilityContinuityIndex * 0.20m) +
                              (adjustedContinuity * 0.80m);

            return decimal.Round(
                d: Math.Clamp(
                    value: blended,
                    min: 0m,
                    max: 1.5m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static double ResolveStableFraction(DistrictId districtId, int salt)
        {
            byte[] bytes = districtId.Value.ToByteArray();
            unchecked
            {
                int hash = 17 + salt;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                return Math.Abs(hash % 1000) / 999d;
            }
        }

        private sealed record DistrictImpactProfile(
            double FloodExposureFactor,
            double MobilityFragilityFactor,
            double UtilityFragilityFactor,
            double SanitationFragilityFactor,
            double SupplyRouteFragilityFactor);
    }
}
