namespace Matrix.SimulationCore.Application.Services.Generation.Abstractions
{
    public interface ICityNameSuggestionService
    {
        IReadOnlyList<string> GetSuggestions(
            string? seed,
            int count);
    }
}
