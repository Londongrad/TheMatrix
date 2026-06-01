using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Generation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Generation
{
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

            Assert.Same(
                expected: firstCities,
                actual: secondCities);
            Assert.Same(
                expected: firstDistricts,
                actual: secondDistricts);
            Assert.Same(
                expected: firstStreets,
                actual: secondStreets);
            Assert.NotEmpty(firstCities);
            Assert.NotEmpty(firstDistricts);
            Assert.NotEmpty(firstStreets);
        }

        [Fact]
        public void Presets_ContainExpectedCanonicalEntries()
        {
            var catalog = new CityGenerationContentCatalog();

            Assert.Contains(
                expected: "Alderhaven",
                collection: catalog.CityNamePresets);
            Assert.Contains(
                expected: "Central Avenue",
                collection: catalog.StreetNamePresets);
            Assert.Contains(
                expected: "Harbor District",
                collection: catalog.DistrictNamePresets);
            Assert.Equal(
                expected: catalog.CityNamePresets.Count,
                actual: catalog.CityNamePresets.Distinct(StringComparer.Ordinal)
                   .Count());
            Assert.Equal(
                expected: catalog.DistrictNamePresets.Count,
                actual: catalog.DistrictNamePresets.Distinct(StringComparer.Ordinal)
                   .Count());
            Assert.Equal(
                expected: catalog.StreetNamePresets.Count,
                actual: catalog.StreetNamePresets.Distinct(StringComparer.Ordinal)
                   .Count());
        }
    }
}
