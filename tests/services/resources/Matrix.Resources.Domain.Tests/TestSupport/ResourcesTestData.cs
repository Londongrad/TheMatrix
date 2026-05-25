using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Simulation;

namespace Matrix.Resources.Domain.Tests.TestSupport
{
    internal static class ResourcesTestData
    {
        internal static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2048,
            month: 6,
            day: 1,
            hour: 8,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        internal static SimulationHostId CreateHostId()
        {
            return new SimulationHostId(Guid.Parse("20000000-0000-0000-0000-000000000001"));
        }

        internal static CityStockpileLineSnapshot CreateLine(
            CityResourceKind kind,
            decimal stockLevelIndex = 0.70m,
            decimal demandPressureIndex = 0.40m,
            decimal resupplyReadinessIndex = 0.60m,
            decimal shortageRiskIndex = 0.30m)
        {
            return new CityStockpileLineSnapshot(
                Kind: kind,
                StockLevelIndex: stockLevelIndex,
                DemandPressureIndex: demandPressureIndex,
                ResupplyReadinessIndex: resupplyReadinessIndex,
                ShortageRiskIndex: shortageRiskIndex);
        }

        internal static CitySystemsResourceDemandSnapshot CreateSystemsDemand(
            decimal fuelDemandPressureIndex = 0.20m,
            decimal sparePartsDemandPressureIndex = 0.30m,
            decimal filtersDemandPressureIndex = 0.25m,
            decimal emergencyWaterDemandPressureIndex = 0.15m,
            decimal overallDemandPressureIndex = 0.22m,
            long effectiveTickId = 5,
            DateTimeOffset? effectiveAtUtc = null)
        {
            return new CitySystemsResourceDemandSnapshot(
                FuelDemandPressureIndex: fuelDemandPressureIndex,
                SparePartsDemandPressureIndex: sparePartsDemandPressureIndex,
                FiltersDemandPressureIndex: filtersDemandPressureIndex,
                EmergencyWaterDemandPressureIndex: emergencyWaterDemandPressureIndex,
                OverallDemandPressureIndex: overallDemandPressureIndex,
                EffectiveTickId: effectiveTickId,
                EffectiveAtUtc: effectiveAtUtc ?? CreatedAtUtc);
        }

        internal static CityOperationalBudgetPressureSnapshot CreateBudgetPressure(
            decimal balance = 500_000m,
            decimal municipalOperationsExpenses = 40_000m,
            decimal generalAvailableAmount = 100_000m,
            decimal operationsAvailableAmount = 90_000m,
            decimal infrastructureAvailableAmount = 80_000m,
            decimal healthcareAvailableAmount = 70_000m,
            string generalAuthorizationLevel = "Medium",
            string operationsAuthorizationLevel = "High",
            string infrastructureAuthorizationLevel = "Medium",
            string healthcareAuthorizationLevel = "Low",
            decimal pressureIndex = 0.35m,
            long effectiveTickId = 5,
            DateTimeOffset? effectiveAtUtc = null)
        {
            return new CityOperationalBudgetPressureSnapshot(
                Balance: balance,
                MunicipalOperationsExpenses: municipalOperationsExpenses,
                GeneralAvailableAmount: generalAvailableAmount,
                OperationsAvailableAmount: operationsAvailableAmount,
                InfrastructureAvailableAmount: infrastructureAvailableAmount,
                HealthcareAvailableAmount: healthcareAvailableAmount,
                GeneralAuthorizationLevel: generalAuthorizationLevel,
                OperationsAuthorizationLevel: operationsAuthorizationLevel,
                InfrastructureAuthorizationLevel: infrastructureAuthorizationLevel,
                HealthcareAuthorizationLevel: healthcareAuthorizationLevel,
                PressureIndex: pressureIndex,
                EffectiveTickId: effectiveTickId,
                EffectiveAtUtc: effectiveAtUtc ?? CreatedAtUtc);
        }

        internal static CityStockpileSnapshot CreateSnapshot(
            DateTimeOffset? evaluatedAtUtc = null,
            bool emergencyRationingEnabled = false,
            decimal supplyStressIndex = 0.33m)
        {
            return new CityStockpileSnapshot(
                Fuel: CreateLine(
                    kind: CityResourceKind.Fuel,
                    stockLevelIndex: 0.64m,
                    demandPressureIndex: 0.52m,
                    resupplyReadinessIndex: 0.55m,
                    shortageRiskIndex: 0.40m),
                Food: CreateLine(
                    kind: CityResourceKind.Food,
                    stockLevelIndex: 0.78m,
                    demandPressureIndex: 0.34m,
                    resupplyReadinessIndex: 0.67m,
                    shortageRiskIndex: 0.24m),
                Medicine: CreateLine(
                    kind: CityResourceKind.Medicine,
                    stockLevelIndex: 0.58m,
                    demandPressureIndex: 0.48m,
                    resupplyReadinessIndex: 0.50m,
                    shortageRiskIndex: 0.45m),
                SpareParts: CreateLine(
                    kind: CityResourceKind.SpareParts,
                    stockLevelIndex: 0.61m,
                    demandPressureIndex: 0.46m,
                    resupplyReadinessIndex: 0.49m,
                    shortageRiskIndex: 0.42m),
                Filters: CreateLine(
                    kind: CityResourceKind.Filters,
                    stockLevelIndex: 0.66m,
                    demandPressureIndex: 0.37m,
                    resupplyReadinessIndex: 0.54m,
                    shortageRiskIndex: 0.36m),
                EmergencyWater: CreateLine(
                    kind: CityResourceKind.EmergencyWater,
                    stockLevelIndex: 0.74m,
                    demandPressureIndex: 0.33m,
                    resupplyReadinessIndex: 0.62m,
                    shortageRiskIndex: 0.28m),
                SystemsDemand: CreateSystemsDemand(effectiveAtUtc: evaluatedAtUtc ?? CreatedAtUtc),
                OperationalBudgetPressure: CreateBudgetPressure(effectiveAtUtc: evaluatedAtUtc ?? CreatedAtUtc),
                SupplyStressIndex: supplyStressIndex,
                EmergencyRationingEnabled: emergencyRationingEnabled,
                EvaluatedAtUtc: evaluatedAtUtc ?? CreatedAtUtc);
        }
    }
}
