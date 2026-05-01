using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityEconomySimulationTemplatePolicyTests
{
    [Fact]
    public void Resolve_WhenClassicCityStrugglingProfileIsRequested_ReturnsStrugglingTemplate()
    {
        var policy = new CityEconomySimulationTemplatePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomySimulationTemplate template = policy.Resolve(
            simulationKind: " classiccity ",
            economyProfile: " struggling ");

        Assert.Equal(CityBudgetUnitKind.Currency, template.UnitProfile.Kind);
        Assert.Equal("MNY", template.UnitProfile.Code);
        Assert.Equal(25_000m, template.InitialReserve.Amount);
        Assert.Equal(7, template.DefaultAllocations.Count);
        Assert.Equal(5, template.DefaultBusinesses.Count);
        Assert.Contains(template.DefaultAllocations, x => x.Category == CityBudgetCategory.Housing && x.TargetAmount.Amount == 5_500m);
        Assert.Contains(template.DefaultBusinesses, x => x.Kind == CityBusinessKind.Landlord && x.StartingCapital.Amount == 7_500m);
    }

    [Fact]
    public void Resolve_WhenMetroSimulationIsRequested_ReturnsMetroCommodityTemplate()
    {
        var policy = new CityEconomySimulationTemplatePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomySimulationTemplate template = policy.Resolve(
            simulationKind: "metro",
            economyProfile: "ignored");

        Assert.Equal(CityBudgetUnitKind.Commodity, template.UnitProfile.Kind);
        Assert.Equal("AMMO", template.UnitProfile.Code);
        Assert.Equal("ctg", template.UnitProfile.Symbol);
        Assert.Equal(12_000m, template.InitialReserve.Amount);
        Assert.Equal(3, template.DefaultAllocations.Count);
        Assert.Equal(3, template.DefaultBusinesses.Count);
        Assert.Contains(template.DefaultBusinesses, x => x.Kind == CityBusinessKind.MunicipalVendor);
        Assert.Contains(template.DefaultBusinesses, x => x.Kind == CityBusinessKind.Manufacturer);
        Assert.Contains(template.DefaultBusinesses, x => x.Kind == CityBusinessKind.RetailStore);
    }

    [Fact]
    public void Resolve_WhenSimulationKindIsUnknown_ReturnsFallbackTemplate()
    {
        var policy = new CityEconomySimulationTemplatePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomySimulationTemplate template = policy.Resolve(
            simulationKind: "unknown",
            economyProfile: "affluent");

        Assert.Equal(CityBudgetUnitKind.Currency, template.UnitProfile.Kind);
        Assert.Equal("MNY", template.UnitProfile.Code);
        Assert.Equal(0m, template.InitialReserve.Amount);
        Assert.Empty(template.DefaultAllocations);
        Assert.Empty(template.DefaultBusinesses);
    }
}
