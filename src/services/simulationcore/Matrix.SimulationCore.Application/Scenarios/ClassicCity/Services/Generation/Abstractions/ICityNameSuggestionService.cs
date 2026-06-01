namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Generation.Abstractions
{
    public interface ICityNameSuggestionService
    {
        IReadOnlyList<string> GetSuggestions(
            string? seed,
            int count);
    }
}
