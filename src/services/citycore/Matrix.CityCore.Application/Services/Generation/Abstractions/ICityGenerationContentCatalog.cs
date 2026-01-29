namespace Matrix.CityCore.Application.Services.Generation.Abstractions
{
    public interface ICityGenerationContentCatalog
    {
        IReadOnlyList<string> CityNamePresets { get; }
        IReadOnlyList<string> DistrictNamePresets { get; }
        IReadOnlyList<string> StreetNamePresets { get; }
    }
}