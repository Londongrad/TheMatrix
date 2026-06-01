using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Generation;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Generation
{
    public sealed class CityNameSuggestionServiceTests
    {
        [Fact]
        public void GetSuggestions_WhenCatalogIsEmpty_ReturnsEmpty()
        {
            var service = new CityNameSuggestionService(
                new ClassicCityTestSupport.FakeCityGenerationContentCatalog
                {
                    CityNamePresets = Array.Empty<string>()
                });

            IReadOnlyList<string> result = service.GetSuggestions(
                seed: "alpha",
                count: 3);

            Assert.Empty(result);
        }

        [Fact]
        public void GetSuggestions_WhenSeedIsBlank_ReturnsDistinctNamesInCatalogOrder()
        {
            var service = new CityNameSuggestionService(
                new ClassicCityTestSupport.FakeCityGenerationContentCatalog
                {
                    CityNamePresets =
                    [
                        "Alderhaven",
                        "alderhaven",
                        "Amberfall",
                        "Blackridge"
                    ]
                });

            IReadOnlyList<string> result = service.GetSuggestions(
                seed: "   ",
                count: 10);

            Assert.Equal(
                expected:
                [
                    "Alderhaven",
                    "Amberfall",
                    "Blackridge"
                ],
                actual: result);
        }

        [Fact]
        public void GetSuggestions_WithSameSeed_IsDeterministic()
        {
            var service = new CityNameSuggestionService(
                new ClassicCityTestSupport.FakeCityGenerationContentCatalog
                {
                    CityNamePresets =
                    [
                        "Alderhaven",
                        "Amberfall",
                        "Ashbourne",
                        "Blackridge",
                        "Blueharbor"
                    ]
                });

            IReadOnlyList<string> first = service.GetSuggestions(
                seed: "neo-seed",
                count: 3);
            IReadOnlyList<string> second = service.GetSuggestions(
                seed: "neo-seed",
                count: 3);

            Assert.Equal(
                expected: first,
                actual: second);
            Assert.Equal(
                expected: 3,
                actual: first.Count);
        }

        [Fact]
        public void GetSuggestions_WithDifferentTrimmedSeedValues_UsesSameShuffle()
        {
            var service = new CityNameSuggestionService(
                new ClassicCityTestSupport.FakeCityGenerationContentCatalog
                {
                    CityNamePresets =
                    [
                        "Alderhaven",
                        "Amberfall",
                        "Ashbourne",
                        "Blackridge",
                        "Blueharbor"
                    ]
                });

            IReadOnlyList<string> first = service.GetSuggestions(
                seed: "seed-42",
                count: 4);
            IReadOnlyList<string> second = service.GetSuggestions(
                seed: "  seed-42  ",
                count: 4);

            Assert.Equal(
                expected: first,
                actual: second);
        }
    }
}
