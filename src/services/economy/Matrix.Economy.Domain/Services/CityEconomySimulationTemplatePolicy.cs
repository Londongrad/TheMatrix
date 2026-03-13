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
                ],
                DefaultBusinesses:
                [
                    new CityEconomyBusinessTemplate("city-housing-authority", "City Housing Authority", CityBusinessKind.Landlord, Money.FromDecimal(7_500m)),
                    new CityEconomyBusinessTemplate("city-utilities-board", "City Utilities Board", CityBusinessKind.Utility, Money.FromDecimal(6_000m)),
                    new CityEconomyBusinessTemplate("municipal-services-desk", "Municipal Services Desk", CityBusinessKind.MunicipalVendor, Money.FromDecimal(5_000m)),
                    new CityEconomyBusinessTemplate("corner-market-coop", "Corner Market Co-op", CityBusinessKind.RetailStore, Money.FromDecimal(3_500m)),
                    new CityEconomyBusinessTemplate("repair-and-works-yard", "Repair and Works Yard", CityBusinessKind.Manufacturer, Money.FromDecimal(4_000m))
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
                ],
                DefaultBusinesses:
                [
                    new CityEconomyBusinessTemplate("city-housing-authority", "City Housing Authority", CityBusinessKind.Landlord, Money.FromDecimal(18_000m)),
                    new CityEconomyBusinessTemplate("city-utilities-board", "City Utilities Board", CityBusinessKind.Utility, Money.FromDecimal(15_000m)),
                    new CityEconomyBusinessTemplate("municipal-services-desk", "Municipal Services Desk", CityBusinessKind.MunicipalVendor, Money.FromDecimal(12_000m)),
                    new CityEconomyBusinessTemplate("market-square-retail", "Market Square Retail", CityBusinessKind.RetailStore, Money.FromDecimal(10_000m)),
                    new CityEconomyBusinessTemplate("civic-services-guild", "Civic Services Guild", CityBusinessKind.Service, Money.FromDecimal(9_000m)),
                    new CityEconomyBusinessTemplate("industrial-works-hub", "Industrial Works Hub", CityBusinessKind.Manufacturer, Money.FromDecimal(11_000m))
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
                ],
                DefaultBusinesses:
                [
                    new CityEconomyBusinessTemplate("city-housing-authority", "City Housing Authority", CityBusinessKind.Landlord, Money.FromDecimal(36_000m)),
                    new CityEconomyBusinessTemplate("city-utilities-board", "City Utilities Board", CityBusinessKind.Utility, Money.FromDecimal(30_000m)),
                    new CityEconomyBusinessTemplate("municipal-services-desk", "Municipal Services Desk", CityBusinessKind.MunicipalVendor, Money.FromDecimal(24_000m)),
                    new CityEconomyBusinessTemplate("market-square-retail", "Market Square Retail", CityBusinessKind.RetailStore, Money.FromDecimal(20_000m)),
                    new CityEconomyBusinessTemplate("civic-services-guild", "Civic Services Guild", CityBusinessKind.Service, Money.FromDecimal(18_000m)),
                    new CityEconomyBusinessTemplate("industrial-works-hub", "Industrial Works Hub", CityBusinessKind.Manufacturer, Money.FromDecimal(22_000m)),
                    new CityEconomyBusinessTemplate("metropolitan-employment-exchange", "Metropolitan Employment Exchange", CityBusinessKind.Employer, Money.FromDecimal(16_000m))
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
                ],
                DefaultBusinesses:
                [
                    new CityEconomyBusinessTemplate("station-quartermaster", "Station Quartermaster", CityBusinessKind.MunicipalVendor, Money.FromDecimal(4_000m)),
                    new CityEconomyBusinessTemplate("tunnel-maintenance-yard", "Tunnel Maintenance Yard", CityBusinessKind.Manufacturer, Money.FromDecimal(3_000m)),
                    new CityEconomyBusinessTemplate("ration-and-supply-depot", "Ration and Supply Depot", CityBusinessKind.RetailStore, Money.FromDecimal(2_500m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildFallbackTemplate()
        {
            return new CityEconomySimulationTemplate(
                UnitProfile: CityBudgetUnitProfile.DefaultMoney(),
                InitialReserve: Money.Zero,
                DefaultAllocations: [],
                DefaultBusinesses: []);
        }

        private static string NormalizeEconomyProfile(string? economyProfile)
        {
            return economyProfile?.Trim().ToUpperInvariant() ?? "BALANCED";
        }
    }
}
