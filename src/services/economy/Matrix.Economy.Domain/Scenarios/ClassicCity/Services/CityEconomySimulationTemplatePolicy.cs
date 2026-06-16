using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityEconomySimulationTemplatePolicy
    {
        public CityEconomySimulationTemplate Resolve(
            string? scenarioKey,
            string? economyProfile)
        {
            return scenarioKey?.Trim()
                   .ToUpperInvariant() switch
            {
                "CLASSICCITY" or "CLASSIC-CITY" => BuildClassicCityTemplate(economyProfile),
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
            var unitProfile = CityBudgetUnitProfile.DefaultMoney();

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                InitialReserve: Money.FromDecimal(25_000m),
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.General,
                        TargetAmount: Money.FromDecimal(2_500m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Operations,
                        TargetAmount: Money.FromDecimal(8_500m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Housing,
                        TargetAmount: Money.FromDecimal(5_500m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Commerce,
                        TargetAmount: Money.FromDecimal(1_500m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Infrastructure,
                        TargetAmount: Money.FromDecimal(6_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Healthcare,
                        TargetAmount: Money.FromDecimal(3_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Education,
                        TargetAmount: Money.FromDecimal(3_000m))
                ],
                DefaultBusinesses:
                [
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "city-housing-authority",
                        Name: "City Housing Authority",
                        Kind: CityBusinessKind.Landlord,
                        StartingCapital: Money.FromDecimal(7_500m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "city-utilities-board",
                        Name: "City Utilities Board",
                        Kind: CityBusinessKind.Utility,
                        StartingCapital: Money.FromDecimal(6_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "municipal-services-desk",
                        Name: "Municipal Services Desk",
                        Kind: CityBusinessKind.MunicipalVendor,
                        StartingCapital: Money.FromDecimal(5_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "corner-market-coop",
                        Name: "Corner Market Co-op",
                        Kind: CityBusinessKind.RetailStore,
                        StartingCapital: Money.FromDecimal(3_500m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "repair-and-works-yard",
                        Name: "Repair and Works Yard",
                        Kind: CityBusinessKind.Manufacturer,
                        StartingCapital: Money.FromDecimal(4_000m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildClassicCityBalancedTemplate()
        {
            var unitProfile = CityBudgetUnitProfile.DefaultMoney();

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                InitialReserve: Money.FromDecimal(75_000m),
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.General,
                        TargetAmount: Money.FromDecimal(3_500m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Operations,
                        TargetAmount: Money.FromDecimal(12_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Housing,
                        TargetAmount: Money.FromDecimal(7_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Commerce,
                        TargetAmount: Money.FromDecimal(2_500m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Infrastructure,
                        TargetAmount: Money.FromDecimal(9_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Healthcare,
                        TargetAmount: Money.FromDecimal(5_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Education,
                        TargetAmount: Money.FromDecimal(5_000m))
                ],
                DefaultBusinesses:
                [
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "city-housing-authority",
                        Name: "City Housing Authority",
                        Kind: CityBusinessKind.Landlord,
                        StartingCapital: Money.FromDecimal(18_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "city-utilities-board",
                        Name: "City Utilities Board",
                        Kind: CityBusinessKind.Utility,
                        StartingCapital: Money.FromDecimal(15_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "municipal-services-desk",
                        Name: "Municipal Services Desk",
                        Kind: CityBusinessKind.MunicipalVendor,
                        StartingCapital: Money.FromDecimal(12_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "market-square-retail",
                        Name: "Market Square Retail",
                        Kind: CityBusinessKind.RetailStore,
                        StartingCapital: Money.FromDecimal(10_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "civic-services-guild",
                        Name: "Civic Services Guild",
                        Kind: CityBusinessKind.Service,
                        StartingCapital: Money.FromDecimal(9_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "industrial-works-hub",
                        Name: "Industrial Works Hub",
                        Kind: CityBusinessKind.Manufacturer,
                        StartingCapital: Money.FromDecimal(11_000m))
                ]);
        }

        private static CityEconomySimulationTemplate BuildClassicCityAffluentTemplate()
        {
            var unitProfile = CityBudgetUnitProfile.DefaultMoney();

            return new CityEconomySimulationTemplate(
                UnitProfile: unitProfile,
                InitialReserve: Money.FromDecimal(180_000m),
                DefaultAllocations:
                [
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.General,
                        TargetAmount: Money.FromDecimal(7_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Operations,
                        TargetAmount: Money.FromDecimal(18_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Housing,
                        TargetAmount: Money.FromDecimal(12_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Commerce,
                        TargetAmount: Money.FromDecimal(6_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Infrastructure,
                        TargetAmount: Money.FromDecimal(16_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Healthcare,
                        TargetAmount: Money.FromDecimal(11_000m)),
                    new CityEconomyAllocationTemplate(
                        Category: CityBudgetCategory.Education,
                        TargetAmount: Money.FromDecimal(11_000m))
                ],
                DefaultBusinesses:
                [
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "city-housing-authority",
                        Name: "City Housing Authority",
                        Kind: CityBusinessKind.Landlord,
                        StartingCapital: Money.FromDecimal(36_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "city-utilities-board",
                        Name: "City Utilities Board",
                        Kind: CityBusinessKind.Utility,
                        StartingCapital: Money.FromDecimal(30_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "municipal-services-desk",
                        Name: "Municipal Services Desk",
                        Kind: CityBusinessKind.MunicipalVendor,
                        StartingCapital: Money.FromDecimal(24_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "market-square-retail",
                        Name: "Market Square Retail",
                        Kind: CityBusinessKind.RetailStore,
                        StartingCapital: Money.FromDecimal(20_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "civic-services-guild",
                        Name: "Civic Services Guild",
                        Kind: CityBusinessKind.Service,
                        StartingCapital: Money.FromDecimal(18_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "industrial-works-hub",
                        Name: "Industrial Works Hub",
                        Kind: CityBusinessKind.Manufacturer,
                        StartingCapital: Money.FromDecimal(22_000m)),
                    new CityEconomyBusinessTemplate(
                        TemplateKey: "metropolitan-employment-exchange",
                        Name: "Metropolitan Employment Exchange",
                        Kind: CityBusinessKind.Employer,
                        StartingCapital: Money.FromDecimal(16_000m))
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
            return economyProfile?.Trim()
                      .ToUpperInvariant() ??
                   "BALANCED";
        }
    }
}
