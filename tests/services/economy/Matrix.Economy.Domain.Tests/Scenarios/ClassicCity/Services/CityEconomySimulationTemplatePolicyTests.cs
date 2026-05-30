using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityEconomySimulationTemplatePolicyTests
    {
        [Fact]
        public void Resolve_WhenClassicCityStrugglingProfileIsRequested_ReturnsStrugglingTemplate()
        {
            var policy = new CityEconomySimulationTemplatePolicy();

            CityEconomySimulationTemplate template = policy.Resolve(
                simulationKind: " classiccity ",
                economyProfile: " struggling ");

            Assert.Equal(
                expected: CityBudgetUnitKind.Currency,
                actual: template.UnitProfile.Kind);
            Assert.Equal(
                expected: "MNY",
                actual: template.UnitProfile.Code);
            Assert.Equal(
                expected: 25_000m,
                actual: template.InitialReserve.Amount);
            Assert.Equal(
                expected: 7,
                actual: template.DefaultAllocations.Count);
            Assert.Equal(
                expected: 5,
                actual: template.DefaultBusinesses.Count);
            Assert.Contains(
                collection: template.DefaultAllocations,
                filter: x => x.Category == CityBudgetCategory.Housing && x.TargetAmount.Amount == 5_500m);
            Assert.Contains(
                collection: template.DefaultBusinesses,
                filter: x => x.Kind == CityBusinessKind.Landlord && x.StartingCapital.Amount == 7_500m);
        }

        [Fact]
        public void Resolve_WhenCanonicalClassicCityKeyIsUsed_ReturnsClassicCityTemplate()
        {
            var policy = new CityEconomySimulationTemplatePolicy();

            CityEconomySimulationTemplate template = policy.Resolve(
                simulationKind: "classic-city",
                economyProfile: "balanced");

            Assert.Equal(
                expected: 75_000m,
                actual: template.InitialReserve.Amount);
        }

        [Fact]
        public void Resolve_WhenMetroSimulationIsRequested_ReturnsMetroCommodityTemplate()
        {
            var policy = new CityEconomySimulationTemplatePolicy();

            CityEconomySimulationTemplate template = policy.Resolve(
                simulationKind: "metro",
                economyProfile: "ignored");

            Assert.Equal(
                expected: CityBudgetUnitKind.Commodity,
                actual: template.UnitProfile.Kind);
            Assert.Equal(
                expected: "AMMO",
                actual: template.UnitProfile.Code);
            Assert.Equal(
                expected: "ctg",
                actual: template.UnitProfile.Symbol);
            Assert.Equal(
                expected: 12_000m,
                actual: template.InitialReserve.Amount);
            Assert.Equal(
                expected: 3,
                actual: template.DefaultAllocations.Count);
            Assert.Equal(
                expected: 3,
                actual: template.DefaultBusinesses.Count);
            Assert.Contains(
                collection: template.DefaultBusinesses,
                filter: x => x.Kind == CityBusinessKind.MunicipalVendor);
            Assert.Contains(
                collection: template.DefaultBusinesses,
                filter: x => x.Kind == CityBusinessKind.Manufacturer);
            Assert.Contains(
                collection: template.DefaultBusinesses,
                filter: x => x.Kind == CityBusinessKind.RetailStore);
        }

        [Fact]
        public void Resolve_WhenSimulationKindIsUnknown_ReturnsFallbackTemplate()
        {
            var policy = new CityEconomySimulationTemplatePolicy();

            CityEconomySimulationTemplate template = policy.Resolve(
                simulationKind: "unknown",
                economyProfile: "affluent");

            Assert.Equal(
                expected: CityBudgetUnitKind.Currency,
                actual: template.UnitProfile.Kind);
            Assert.Equal(
                expected: "MNY",
                actual: template.UnitProfile.Code);
            Assert.Equal(
                expected: 0m,
                actual: template.InitialReserve.Amount);
            Assert.Empty(template.DefaultAllocations);
            Assert.Empty(template.DefaultBusinesses);
        }
    }
}
