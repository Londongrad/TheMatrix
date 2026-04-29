using Matrix.SimulationCore.Application.Services.Generation;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Generation;

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

        IReadOnlyList<string> result = service.GetSuggestions("alpha", 3);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSuggestions_WhenSeedIsBlank_ReturnsDistinctNamesInCatalogOrder()
    {
        var service = new CityNameSuggestionService(
            new ClassicCityTestSupport.FakeCityGenerationContentCatalog
            {
                CityNamePresets = ["Alderhaven", "alderhaven", "Amberfall", "Blackridge"]
            });

        IReadOnlyList<string> result = service.GetSuggestions("   ", 10);

        Assert.Equal(["Alderhaven", "Amberfall", "Blackridge"], result);
    }

    [Fact]
    public void GetSuggestions_WithSameSeed_IsDeterministic()
    {
        var service = new CityNameSuggestionService(
            new ClassicCityTestSupport.FakeCityGenerationContentCatalog
            {
                CityNamePresets = ["Alderhaven", "Amberfall", "Ashbourne", "Blackridge", "Blueharbor"]
            });

        IReadOnlyList<string> first = service.GetSuggestions("neo-seed", 3);
        IReadOnlyList<string> second = service.GetSuggestions("neo-seed", 3);

        Assert.Equal(first, second);
        Assert.Equal(3, first.Count);
    }

    [Fact]
    public void GetSuggestions_WithDifferentTrimmedSeedValues_UsesSameShuffle()
    {
        var service = new CityNameSuggestionService(
            new ClassicCityTestSupport.FakeCityGenerationContentCatalog
            {
                CityNamePresets = ["Alderhaven", "Amberfall", "Ashbourne", "Blackridge", "Blueharbor"]
            });

        IReadOnlyList<string> first = service.GetSuggestions("seed-42", 4);
        IReadOnlyList<string> second = service.GetSuggestions("  seed-42  ", 4);

        Assert.Equal(first, second);
    }
}
