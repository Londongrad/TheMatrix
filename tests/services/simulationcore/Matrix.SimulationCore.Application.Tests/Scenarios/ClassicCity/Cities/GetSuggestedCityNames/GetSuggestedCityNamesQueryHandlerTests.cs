using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetSuggestedCityNames
{
    public sealed class GetSuggestedCityNamesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsSuggestionsAndPreservesSeed()
        {
            var suggestionService = new ClassicCityTestSupport.FakeCityNameSuggestionService
            {
                Result =
                [
                    "Alpha",
                    "Beta",
                    "Gamma"
                ]
            };
            var handler = new GetSuggestedCityNamesQueryHandler(suggestionService);

            SuggestedCityNamesDto result = await handler.Handle(
                request: new GetSuggestedCityNamesQuery(
                    Seed: "seed-42",
                    Count: 3),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "seed-42",
                actual: result.Seed);
            Assert.Equal(
                expected:
                [
                    "Alpha",
                    "Beta",
                    "Gamma"
                ],
                actual: result.Names);
            Assert.Equal(
                expected: "seed-42",
                actual: suggestionService.RequestedSeed);
            Assert.Equal(
                expected: 3,
                actual: suggestionService.RequestedCount);
        }

        [Fact]
        public async Task Handle_WhenSeedIsNull_StillDelegatesToSuggestionService()
        {
            var suggestionService = new ClassicCityTestSupport.FakeCityNameSuggestionService
            {
                Result = ["Delta"]
            };
            var handler = new GetSuggestedCityNamesQueryHandler(suggestionService);

            SuggestedCityNamesDto result = await handler.Handle(
                request: new GetSuggestedCityNamesQuery(
                    Seed: null,
                    Count: 1),
                cancellationToken: CancellationToken.None);

            Assert.Null(result.Seed);
            Assert.Equal(
                expected: ["Delta"],
                actual: result.Names);
            Assert.Null(suggestionService.RequestedSeed);
            Assert.Equal(
                expected: 1,
                actual: suggestionService.RequestedCount);
        }
    }
}
