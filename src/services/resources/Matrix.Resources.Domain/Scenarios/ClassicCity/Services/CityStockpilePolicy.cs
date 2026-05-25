using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityStockpilePolicy
    {
        public CityStockpileSnapshot CreateSeed(
            string? developmentLevel,
            DateTimeOffset createdAtUtc)
        {
            ResourceSeedProfile profile = ResolveSeedProfile(developmentLevel);

            CityStockpileLineSnapshot fuel = CreateSeedLine(
                kind: CityResourceKind.Fuel,
                profile: profile,
                stockAdjustment: -0.06m,
                demandAdjustment: 0.08m,
                readinessAdjustment: -0.04m,
                fragility: 0.82m);
            CityStockpileLineSnapshot food = CreateSeedLine(
                kind: CityResourceKind.Food,
                profile: profile,
                stockAdjustment: 0.04m,
                demandAdjustment: 0.02m,
                readinessAdjustment: 0.03m,
                fragility: 0.52m);
            CityStockpileLineSnapshot medicine = CreateSeedLine(
                kind: CityResourceKind.Medicine,
                profile: profile,
                stockAdjustment: -0.08m,
                demandAdjustment: 0.06m,
                readinessAdjustment: -0.05m,
                fragility: 0.76m);
            CityStockpileLineSnapshot spareParts = CreateSeedLine(
                kind: CityResourceKind.SpareParts,
                profile: profile,
                stockAdjustment: -0.10m,
                demandAdjustment: 0.03m,
                readinessAdjustment: -0.07m,
                fragility: 0.90m);
            CityStockpileLineSnapshot filters = CreateSeedLine(
                kind: CityResourceKind.Filters,
                profile: profile,
                stockAdjustment: -0.04m,
                demandAdjustment: 0.04m,
                readinessAdjustment: -0.03m,
                fragility: 0.84m);
            CityStockpileLineSnapshot emergencyWater = CreateSeedLine(
                kind: CityResourceKind.EmergencyWater,
                profile: profile,
                stockAdjustment: -0.02m,
                demandAdjustment: 0.05m,
                readinessAdjustment: -0.01m,
                fragility: 0.64m);

            return new CityStockpileSnapshot(
                Fuel: fuel,
                Food: food,
                Medicine: medicine,
                SpareParts: spareParts,
                Filters: filters,
                EmergencyWater: emergencyWater,
                SystemsDemand: CitySystemsResourceDemandSnapshot.Neutral(createdAtUtc),
                OperationalBudgetPressure: CityOperationalBudgetPressureSnapshot.Neutral(createdAtUtc),
                SupplyStressIndex: CalculateSupplyStress(
                    fuel: fuel,
                    food: food,
                    medicine: medicine,
                    spareParts: spareParts,
                    filters: filters,
                    emergencyWater: emergencyWater,
                    emergencyRationingEnabled: false),
                EmergencyRationingEnabled: false,
                EvaluatedAtUtc: EnsureUtc(createdAtUtc));
        }

        public CityStockpileSnapshot Advance(
            CityStockpileSnapshot current,
            TimeSpan elapsed)
        {
            ArgumentNullException.ThrowIfNull(current);

            if (elapsed <= TimeSpan.Zero)
                return current;

            decimal elapsedDays = decimal.Round(
                d: (decimal)elapsed.TotalMinutes / 1440m,
                decimals: 6,
                mode: MidpointRounding.AwayFromZero);
            decimal operationalDemandDays = CalculateOperationalDemandDays(
                fromUtc: current.EvaluatedAtUtc,
                toUtc: current.EvaluatedAtUtc.Add(elapsed),
                demandEffectiveAtUtc: current.SystemsDemand.EffectiveAtUtc);

            CityStockpileLineSnapshot fuel = AdvanceLine(
                line: current.Fuel,
                elapsedDays: elapsedDays,
                systemsDemandPressure: current.SystemsDemand.FuelDemandPressureIndex,
                systemsDemandElapsedDays: operationalDemandDays,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                demandDrainRate: 0.044m,
                passiveResupplyRate: 0.030m,
                fragility: 0.82m);
            CityStockpileLineSnapshot food = AdvanceLine(
                line: current.Food,
                elapsedDays: elapsedDays,
                systemsDemandPressure: 0m,
                systemsDemandElapsedDays: 0m,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                demandDrainRate: 0.034m,
                passiveResupplyRate: 0.027m,
                fragility: 0.52m);
            CityStockpileLineSnapshot medicine = AdvanceLine(
                line: current.Medicine,
                elapsedDays: elapsedDays,
                systemsDemandPressure: 0m,
                systemsDemandElapsedDays: 0m,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                demandDrainRate: 0.027m,
                passiveResupplyRate: 0.019m,
                fragility: 0.76m);
            CityStockpileLineSnapshot spareParts = AdvanceLine(
                line: current.SpareParts,
                elapsedDays: elapsedDays,
                systemsDemandPressure: current.SystemsDemand.SparePartsDemandPressureIndex,
                systemsDemandElapsedDays: operationalDemandDays,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                demandDrainRate: 0.019m,
                passiveResupplyRate: 0.015m,
                fragility: 0.90m);
            CityStockpileLineSnapshot filters = AdvanceLine(
                line: current.Filters,
                elapsedDays: elapsedDays,
                systemsDemandPressure: current.SystemsDemand.FiltersDemandPressureIndex,
                systemsDemandElapsedDays: operationalDemandDays,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                demandDrainRate: 0.021m,
                passiveResupplyRate: 0.017m,
                fragility: 0.84m);
            CityStockpileLineSnapshot emergencyWater = AdvanceLine(
                line: current.EmergencyWater,
                elapsedDays: elapsedDays,
                systemsDemandPressure: current.SystemsDemand.EmergencyWaterDemandPressureIndex,
                systemsDemandElapsedDays: operationalDemandDays,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                demandDrainRate: 0.030m,
                passiveResupplyRate: 0.021m,
                fragility: 0.64m);

            return new CityStockpileSnapshot(
                Fuel: fuel,
                Food: food,
                Medicine: medicine,
                SpareParts: spareParts,
                Filters: filters,
                EmergencyWater: emergencyWater,
                SystemsDemand: current.SystemsDemand,
                OperationalBudgetPressure: current.OperationalBudgetPressure,
                SupplyStressIndex: CalculateSupplyStress(
                    fuel: fuel,
                    food: food,
                    medicine: medicine,
                    spareParts: spareParts,
                    filters: filters,
                    emergencyWater: emergencyWater,
                    emergencyRationingEnabled: current.EmergencyRationingEnabled),
                EmergencyRationingEnabled: current.EmergencyRationingEnabled,
                EvaluatedAtUtc: EnsureUtc(current.EvaluatedAtUtc.Add(elapsed)));
        }

        public CityStockpileSnapshot SetEmergencyRationing(
            CityStockpileSnapshot current,
            bool enabled)
        {
            ArgumentNullException.ThrowIfNull(current);

            return new CityStockpileSnapshot(
                Fuel: current.Fuel,
                Food: current.Food,
                Medicine: current.Medicine,
                SpareParts: current.SpareParts,
                Filters: current.Filters,
                EmergencyWater: current.EmergencyWater,
                SystemsDemand: current.SystemsDemand,
                OperationalBudgetPressure: current.OperationalBudgetPressure,
                SupplyStressIndex: CalculateSupplyStress(
                    fuel: current.Fuel,
                    food: current.Food,
                    medicine: current.Medicine,
                    spareParts: current.SpareParts,
                    filters: current.Filters,
                    emergencyWater: current.EmergencyWater,
                    emergencyRationingEnabled: enabled),
                EmergencyRationingEnabled: enabled,
                EvaluatedAtUtc: current.EvaluatedAtUtc);
        }

        public CityStockpileSnapshot DispatchResupply(
            CityStockpileSnapshot current,
            ResupplyFocus focus,
            ResupplyIntensity intensity)
        {
            ArgumentNullException.ThrowIfNull(current);

            decimal stockBoost = intensity switch
            {
                ResupplyIntensity.Low => 0.08m,
                ResupplyIntensity.Medium => 0.14m,
                ResupplyIntensity.High => 0.22m,
                _ => 0.12m
            };
            decimal readinessBoost = intensity switch
            {
                ResupplyIntensity.Low => 0.05m,
                ResupplyIntensity.Medium => 0.09m,
                ResupplyIntensity.High => 0.14m,
                _ => 0.07m
            };
            decimal riskRelief = intensity switch
            {
                ResupplyIntensity.Low => 0.06m,
                ResupplyIntensity.Medium => 0.11m,
                ResupplyIntensity.High => 0.17m,
                _ => 0.08m
            };

            CityStockpileLineSnapshot fuel = ApplyResupply(
                line: current.Fuel,
                focus: focus,
                stockBoost: stockBoost,
                readinessBoost: readinessBoost,
                riskRelief: riskRelief);
            CityStockpileLineSnapshot food = ApplyResupply(
                line: current.Food,
                focus: focus,
                stockBoost: stockBoost,
                readinessBoost: readinessBoost,
                riskRelief: riskRelief);
            CityStockpileLineSnapshot medicine = ApplyResupply(
                line: current.Medicine,
                focus: focus,
                stockBoost: stockBoost,
                readinessBoost: readinessBoost,
                riskRelief: riskRelief);
            CityStockpileLineSnapshot spareParts = ApplyResupply(
                line: current.SpareParts,
                focus: focus,
                stockBoost: stockBoost,
                readinessBoost: readinessBoost,
                riskRelief: riskRelief);
            CityStockpileLineSnapshot filters = ApplyResupply(
                line: current.Filters,
                focus: focus,
                stockBoost: stockBoost,
                readinessBoost: readinessBoost,
                riskRelief: riskRelief);
            CityStockpileLineSnapshot emergencyWater = ApplyResupply(
                line: current.EmergencyWater,
                focus: focus,
                stockBoost: stockBoost,
                readinessBoost: readinessBoost,
                riskRelief: riskRelief);

            return new CityStockpileSnapshot(
                Fuel: fuel,
                Food: food,
                Medicine: medicine,
                SpareParts: spareParts,
                Filters: filters,
                EmergencyWater: emergencyWater,
                SystemsDemand: current.SystemsDemand,
                OperationalBudgetPressure: current.OperationalBudgetPressure,
                SupplyStressIndex: CalculateSupplyStress(
                    fuel: fuel,
                    food: food,
                    medicine: medicine,
                    spareParts: spareParts,
                    filters: filters,
                    emergencyWater: emergencyWater,
                    emergencyRationingEnabled: current.EmergencyRationingEnabled),
                EmergencyRationingEnabled: current.EmergencyRationingEnabled,
                EvaluatedAtUtc: current.EvaluatedAtUtc);
        }

        public CityStockpileSnapshot ApplySystemsDemand(CityStockpileSnapshot current)
        {
            ArgumentNullException.ThrowIfNull(current);

            if (current.SystemsDemand.EffectiveAtUtc > current.EvaluatedAtUtc)
                return current;

            CityStockpileLineSnapshot fuel = ApplyOperationalDemand(
                line: current.Fuel,
                systemsDemandPressure: current.SystemsDemand.FuelDemandPressureIndex,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                fragility: 0.82m);
            CityStockpileLineSnapshot spareParts = ApplyOperationalDemand(
                line: current.SpareParts,
                systemsDemandPressure: current.SystemsDemand.SparePartsDemandPressureIndex,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                fragility: 0.90m);
            CityStockpileLineSnapshot filters = ApplyOperationalDemand(
                line: current.Filters,
                systemsDemandPressure: current.SystemsDemand.FiltersDemandPressureIndex,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                fragility: 0.84m);
            CityStockpileLineSnapshot emergencyWater = ApplyOperationalDemand(
                line: current.EmergencyWater,
                systemsDemandPressure: current.SystemsDemand.EmergencyWaterDemandPressureIndex,
                emergencyRationingEnabled: current.EmergencyRationingEnabled,
                fragility: 0.64m);

            return new CityStockpileSnapshot(
                Fuel: fuel,
                Food: current.Food,
                Medicine: current.Medicine,
                SpareParts: spareParts,
                Filters: filters,
                EmergencyWater: emergencyWater,
                SystemsDemand: current.SystemsDemand,
                OperationalBudgetPressure: current.OperationalBudgetPressure,
                SupplyStressIndex: CalculateSupplyStress(
                    fuel: fuel,
                    food: current.Food,
                    medicine: current.Medicine,
                    spareParts: spareParts,
                    filters: filters,
                    emergencyWater: emergencyWater,
                    emergencyRationingEnabled: current.EmergencyRationingEnabled),
                EmergencyRationingEnabled: current.EmergencyRationingEnabled,
                EvaluatedAtUtc: current.EvaluatedAtUtc);
        }

        private static CityStockpileLineSnapshot CreateSeedLine(
            CityResourceKind kind,
            ResourceSeedProfile profile,
            decimal stockAdjustment,
            decimal demandAdjustment,
            decimal readinessAdjustment,
            decimal fragility)
        {
            decimal stock = ClampIndex(profile.StockBase + stockAdjustment);
            decimal demand = ClampIndex(profile.DemandBase + demandAdjustment);
            decimal readiness = ClampIndex(profile.ReadinessBase + readinessAdjustment);
            decimal risk = CalculateShortageRisk(
                stockLevelIndex: stock,
                demandPressureIndex: demand,
                resupplyReadinessIndex: readiness,
                emergencyRationingEnabled: false,
                fragility: fragility);

            return new CityStockpileLineSnapshot(
                Kind: kind,
                StockLevelIndex: stock,
                DemandPressureIndex: demand,
                ResupplyReadinessIndex: readiness,
                ShortageRiskIndex: risk);
        }

        private static CityStockpileLineSnapshot AdvanceLine(
            CityStockpileLineSnapshot line,
            decimal elapsedDays,
            decimal systemsDemandPressure,
            decimal systemsDemandElapsedDays,
            bool emergencyRationingEnabled,
            decimal demandDrainRate,
            decimal passiveResupplyRate,
            decimal fragility)
        {
            decimal rationingRelief = emergencyRationingEnabled
                ? 0.18m
                : 0m;
            decimal effectiveDemand = ClampIndex(line.DemandPressureIndex - rationingRelief);
            decimal operationalDemandBoost = systemsDemandPressure * systemsDemandElapsedDays;

            decimal naturalResupply =
                passiveResupplyRate * elapsedDays * (0.55m + (line.ResupplyReadinessIndex * 0.75m));
            decimal consumption = demandDrainRate *
                                  elapsedDays *
                                  (0.65m + (effectiveDemand * 0.85m) + (operationalDemandBoost * 0.90m));
            decimal stock = ClampIndex(line.StockLevelIndex + naturalResupply - consumption);

            decimal readinessDecay = 0.018m * elapsedDays * (0.40m + fragility + (effectiveDemand * 0.35m));
            readinessDecay += 0.010m * operationalDemandBoost;
            decimal readinessRecovery = 0.012m * elapsedDays * (1m - line.ShortageRiskIndex);
            decimal readiness = ClampIndex(line.ResupplyReadinessIndex - readinessDecay + readinessRecovery);

            decimal demandDrift = (0.010m * elapsedDays * fragility) -
                                  (emergencyRationingEnabled
                                      ? 0.028m * elapsedDays
                                      : 0m) +
                                  (0.030m * operationalDemandBoost);
            decimal demand = ClampIndex(line.DemandPressureIndex + demandDrift);

            decimal risk = CalculateShortageRisk(
                stockLevelIndex: stock,
                demandPressureIndex: demand,
                resupplyReadinessIndex: readiness,
                emergencyRationingEnabled: emergencyRationingEnabled,
                fragility: fragility);

            return new CityStockpileLineSnapshot(
                Kind: line.Kind,
                StockLevelIndex: stock,
                DemandPressureIndex: demand,
                ResupplyReadinessIndex: readiness,
                ShortageRiskIndex: risk);
        }

        private static CityStockpileLineSnapshot ApplyOperationalDemand(
            CityStockpileLineSnapshot line,
            decimal systemsDemandPressure,
            bool emergencyRationingEnabled,
            decimal fragility)
        {
            decimal demand = ClampIndex(line.DemandPressureIndex + (systemsDemandPressure * 0.1800m));
            decimal readiness = ClampIndex(line.ResupplyReadinessIndex - (systemsDemandPressure * 0.0800m));
            decimal risk = CalculateShortageRisk(
                stockLevelIndex: line.StockLevelIndex,
                demandPressureIndex: demand,
                resupplyReadinessIndex: readiness,
                emergencyRationingEnabled: emergencyRationingEnabled,
                fragility: fragility);

            return new CityStockpileLineSnapshot(
                Kind: line.Kind,
                StockLevelIndex: line.StockLevelIndex,
                DemandPressureIndex: demand,
                ResupplyReadinessIndex: readiness,
                ShortageRiskIndex: risk);
        }

        private static CityStockpileLineSnapshot ApplyResupply(
            CityStockpileLineSnapshot line,
            ResupplyFocus focus,
            decimal stockBoost,
            decimal readinessBoost,
            decimal riskRelief)
        {
            bool isFocused = focus == ResupplyFocus.All || focus == MapFocus(line.Kind);
            decimal multiplier = isFocused
                ? 1m
                : 0.35m;

            decimal stock = ClampIndex(line.StockLevelIndex + (stockBoost * multiplier));
            decimal readiness = ClampIndex(line.ResupplyReadinessIndex + (readinessBoost * multiplier));
            decimal risk = ClampIndex(line.ShortageRiskIndex - (riskRelief * multiplier));

            return new CityStockpileLineSnapshot(
                Kind: line.Kind,
                StockLevelIndex: stock,
                DemandPressureIndex: line.DemandPressureIndex,
                ResupplyReadinessIndex: readiness,
                ShortageRiskIndex: risk);
        }

        private static decimal CalculateSupplyStress(
            CityStockpileLineSnapshot fuel,
            CityStockpileLineSnapshot food,
            CityStockpileLineSnapshot medicine,
            CityStockpileLineSnapshot spareParts,
            CityStockpileLineSnapshot filters,
            CityStockpileLineSnapshot emergencyWater,
            bool emergencyRationingEnabled)
        {
            decimal weightedStress =
                (fuel.ShortageRiskIndex * 0.18m) +
                (food.ShortageRiskIndex * 0.22m) +
                (medicine.ShortageRiskIndex * 0.16m) +
                (spareParts.ShortageRiskIndex * 0.12m) +
                (filters.ShortageRiskIndex * 0.10m) +
                (emergencyWater.ShortageRiskIndex * 0.22m);

            decimal rationingRelief = emergencyRationingEnabled
                ? 0.05m
                : 0m;

            return ClampIndex(weightedStress - rationingRelief);
        }

        private static decimal CalculateShortageRisk(
            decimal stockLevelIndex,
            decimal demandPressureIndex,
            decimal resupplyReadinessIndex,
            bool emergencyRationingEnabled,
            decimal fragility)
        {
            decimal rationingRelief = emergencyRationingEnabled
                ? 0.07m
                : 0m;

            return ClampIndex(
                value: 0.14m +
                       ((1m - stockLevelIndex) * 0.42m) +
                       (demandPressureIndex * 0.22m) +
                       ((1m - resupplyReadinessIndex) * 0.16m) +
                       (fragility * 0.12m) -
                       rationingRelief);
        }

        private static ResupplyFocus MapFocus(CityResourceKind kind)
        {
            return kind switch
            {
                CityResourceKind.Fuel => ResupplyFocus.Fuel,
                CityResourceKind.Food => ResupplyFocus.Food,
                CityResourceKind.Medicine => ResupplyFocus.Medicine,
                CityResourceKind.SpareParts => ResupplyFocus.SpareParts,
                CityResourceKind.Filters => ResupplyFocus.Filters,
                CityResourceKind.EmergencyWater => ResupplyFocus.EmergencyWater,
                _ => ResupplyFocus.All
            };
        }

        private static ResourceSeedProfile ResolveSeedProfile(string? developmentLevel)
        {
            string normalized = string.IsNullOrWhiteSpace(developmentLevel)
                ? string.Empty
                : developmentLevel.Trim();

            return normalized.ToLowerInvariant() switch
            {
                "struggling" => new ResourceSeedProfile(
                    StockBase: 0.56m,
                    DemandBase: 0.82m,
                    ReadinessBase: 0.44m),
                "advanced" => new ResourceSeedProfile(
                    StockBase: 0.79m,
                    DemandBase: 0.55m,
                    ReadinessBase: 0.80m),
                "affluent" => new ResourceSeedProfile(
                    StockBase: 0.88m,
                    DemandBase: 0.48m,
                    ReadinessBase: 0.89m),
                _ => new ResourceSeedProfile(
                    StockBase: 0.68m,
                    DemandBase: 0.68m,
                    ReadinessBase: 0.62m)
            };
        }

        private static decimal ClampIndex(decimal value)
        {
            return decimal.Round(
                d: Math.Min(
                    val1: 1m,
                    val2: Math.Max(
                        val1: 0m,
                        val2: value)),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal CalculateOperationalDemandDays(
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            DateTimeOffset demandEffectiveAtUtc)
        {
            if (demandEffectiveAtUtc >= toUtc)
                return 0m;

            DateTimeOffset effectiveFrom = demandEffectiveAtUtc > fromUtc
                ? demandEffectiveAtUtc
                : fromUtc;

            if (effectiveFrom >= toUtc)
                return 0m;

            return decimal.Round(
                d: (decimal)(toUtc - effectiveFrom).TotalMinutes / 1440m,
                decimals: 6,
                mode: MidpointRounding.AwayFromZero);
        }

        private static DateTimeOffset EnsureUtc(DateTimeOffset value)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : value.ToUniversalTime();
        }

        private readonly record struct ResourceSeedProfile(
            decimal StockBase,
            decimal DemandBase,
            decimal ReadinessBase);
    }
}
