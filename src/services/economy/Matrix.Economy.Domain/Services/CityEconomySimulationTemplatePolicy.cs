using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Services
{
    public sealed class CityEconomySimulationTemplatePolicy
    {
        public CityEconomySimulationTemplate Resolve(string? simulationKind)
        {
            return simulationKind?.Trim().ToUpperInvariant() switch
            {
                "CLASSICCITY" => BuildClassicCityTemplate(),
                "METRO" => BuildMetroTemplate(),
                _ => BuildFallbackTemplate()
            };
        }

        private static CityEconomySimulationTemplate BuildClassicCityTemplate()
        {
            CityBudgetUnitProfile unitProfile = CityBudgetUnitProfile.DefaultMoney();

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(CityBudgetCategory.General, Money.FromDecimal(3500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Operations, Money.FromDecimal(12000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Housing, Money.FromDecimal(7000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Commerce, Money.FromDecimal(2500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Infrastructure, Money.FromDecimal(9000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Healthcare, Money.FromDecimal(5000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Education, Money.FromDecimal(5000m))
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
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(CityBudgetCategory.General, Money.FromDecimal(1500m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Operations, Money.FromDecimal(4000m)),
                    new CityEconomyAllocationTemplate(CityBudgetCategory.Infrastructure, Money.FromDecimal(3000m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildFallbackTemplate()
        {
            return new CityEconomySimulationTemplate(
                UnitProfile: CityBudgetUnitProfile.DefaultMoney(),
                DefaultAllocations: []);
        }
    }
}
