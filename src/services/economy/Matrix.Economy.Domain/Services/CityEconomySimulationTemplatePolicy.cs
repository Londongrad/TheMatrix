using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Services
{
    public sealed class CityEconomySimulationTemplatePolicy
    {
        public CityEconomySimulationTemplate Resolve(
            string? simulationKind,
            string? economyProfile)
        {
            return simulationKind?.Trim().ToUpperInvariant() switch
            {
                "CLASSICCITY" => BuildClassicCityTemplate(economyProfile),
                "METRO" => BuildMetroTemplate(),
                _ => BuildFallbackTemplate()
            };
        }

        private static CityEconomySimulationTemplate BuildClassicCityTemplate(string? economyProfile)
        {
            return NormalizeEconomyProfile(economyProfile) switch
            {
                "STRUGGLING" => BuildClassicCityStrugglingTemplate(),
                "AFFLUENT" => BuildClassicCityAffluentTemplate(),
                _ => BuildClassicCityBalancedTemplate()
            };
        }

        private static CityEconomySimulationTemplate BuildClassicCityStrugglingTemplate()
        {
            CityBudgetUnitProfile unitProfile = CityBudgetUnitProfile.DefaultMoney();

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                InitialReserve: Money.FromDecimal(25_000m),
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(CityBudgetCategory.General, Money.FromDecimal(2_500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Operations, Money.FromDecimal(8_500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Housing, Money.FromDecimal(5_500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Commerce, Money.FromDecimal(1_500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Infrastructure, Money.FromDecimal(6_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Healthcare, Money.FromDecimal(3_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Education, Money.FromDecimal(3_000m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildClassicCityBalancedTemplate()
        {
            CityBudgetUnitProfile unitProfile = CityBudgetUnitProfile.DefaultMoney();

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                InitialReserve: Money.FromDecimal(75_000m),
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(CityBudgetCategory.General, Money.FromDecimal(3_500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Operations, Money.FromDecimal(12_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Housing, Money.FromDecimal(7_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Commerce, Money.FromDecimal(2_500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Infrastructure, Money.FromDecimal(9_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Healthcare, Money.FromDecimal(5_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Education, Money.FromDecimal(5_000m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildClassicCityAffluentTemplate()
        {
            CityBudgetUnitProfile unitProfile = CityBudgetUnitProfile.DefaultMoney();

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                InitialReserve: Money.FromDecimal(180_000m),
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(CityBudgetCategory.General, Money.FromDecimal(7_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Operations, Money.FromDecimal(18_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Housing, Money.FromDecimal(12_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Commerce, Money.FromDecimal(6_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Infrastructure, Money.FromDecimal(16_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Healthcare, Money.FromDecimal(11_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Education, Money.FromDecimal(11_000m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildMetroTemplate()
        {
            var unitProfile = new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Commodity,
                Code: "AMMO",
                DisplayName: "Cartridges",
                Symbol: "ctg");

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                InitialReserve: Money.FromDecimal(12_000m),
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(CityBudgetCategory.General, Money.FromDecimal(1_500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Operations, Money.FromDecimal(4_000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Infrastructure, Money.FromDecimal(3_000m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildFallbackTemplate()
        {
            return new CityEconomySimulationTemplate(
                UnitProfile: CityBudgetUnitProfile.DefaultMoney(),
                InitialReserve: Money.Zero,
                DefaultAllocations: []);
        }

        private static string NormalizeEconomyProfile(string? economyProfile)
        {
            return economyProfile?.Trim().ToUpperInvariant() ?? "BALANCED";
        }
    }
}
