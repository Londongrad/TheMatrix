using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetSuggestedCityNames;

public sealed class GetSuggestedCityNamesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSuggestionsAndPreservesSeed()
    {
        var suggestionService = new ClassicCityTestSupport.FakeCityNameSuggestionService
        {
            Result = ["Alpha", "Beta", "Gamma"]
        };
        var handler = new GetSuggestedCityNamesQueryHandler(suggestionService);

        var result = await handler.Handle(new GetSuggestedCityNamesQuery("seed-42", 3), CancellationToken.None);

        Assert.Equal("seed-42", result.Seed);
        Assert.Equal(["Alpha", "Beta", "Gamma"], result.Names);
        Assert.Equal("seed-42", suggestionService.RequestedSeed);
        Assert.Equal(3, suggestionService.RequestedCount);
    }

    [Fact]
    public async Task Handle_WhenSeedIsNull_StillDelegatesToSuggestionService()
    {
        var suggestionService = new ClassicCityTestSupport.FakeCityNameSuggestionService
        {
            Result = ["Delta"]
        };
        var handler = new GetSuggestedCityNamesQueryHandler(suggestionService);

        var result = await handler.Handle(new GetSuggestedCityNamesQuery(null, 1), CancellationToken.None);

        Assert.Null(result.Seed);
        Assert.Equal(["Delta"], result.Names);
        Assert.Null(suggestionService.RequestedSeed);
        Assert.Equal(1, suggestionService.RequestedCount);
    }
}
