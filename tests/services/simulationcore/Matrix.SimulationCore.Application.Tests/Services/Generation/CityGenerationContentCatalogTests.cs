using Matrix.SimulationCore.Application.Services.Generation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Generation;

public sealed class CityGenerationContentCatalogTests
{
    [Fact]
    public void Presets_ArePopulatedAndStableAcrossReads()
    {
        var catalog = new CityGenerationContentCatalog();

        IReadOnlyList<string> firstCities = catalog.CityNamePresets;
        IReadOnlyList<string> secondCities = catalog.CityNamePresets;
        IReadOnlyList<string> firstDistricts = catalog.DistrictNamePresets;
        IReadOnlyList<string> secondDistricts = catalog.DistrictNamePresets;
        IReadOnlyList<string> firstStreets = catalog.StreetNamePresets;
        IReadOnlyList<string> secondStreets = catalog.StreetNamePresets;

        Assert.Same(firstCities, secondCities);
        Assert.Same(firstDistricts, secondDistricts);
        Assert.Same(firstStreets, secondStreets);
        Assert.NotEmpty(firstCities);
        Assert.NotEmpty(firstDistricts);
        Assert.NotEmpty(firstStreets);
    }

    [Fact]
    public void Presets_ContainExpectedCanonicalEntries()
    {
        var catalog = new CityGenerationContentCatalog();

        Assert.Contains("Alderhaven", catalog.CityNamePresets);
        Assert.Contains("Central Avenue", catalog.StreetNamePresets);
        Assert.Contains("Harbor District", catalog.DistrictNamePresets);
        Assert.Equal(catalog.CityNamePresets.Count, catalog.CityNamePresets.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(catalog.DistrictNamePresets.Count, catalog.DistrictNamePresets.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(catalog.StreetNamePresets.Count, catalog.StreetNamePresets.Distinct(StringComparer.Ordinal).Count());
    }
}
