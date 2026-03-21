using Matrix.SimulationCore.Application.Services.Generation.Abstractions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames
{
    public sealed class GetSuggestedCityNamesQueryHandler(ICityNameSuggestionService suggestionService)
        : IRequestHandler<GetSuggestedCityNamesQuery, SuggestedCityNamesDto>
    {
        public Task<SuggestedCityNamesDto> Handle(
            GetSuggestedCityNamesQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<string> names = suggestionService.GetSuggestions(
                seed: request.Seed,
                count: request.Count);

            return Task.FromResult(
                new SuggestedCityNamesDto(
                    Seed: request.Seed,
                    Names: names));
        }
    }
}
