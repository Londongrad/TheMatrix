using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions
{
    public interface IPopulationGenerationContentCatalog
    {
        IReadOnlyList<string> MaleFirstNames { get; }
        IReadOnlyList<string> FemaleFirstNames { get; }
        IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames { get; }
        IReadOnlyList<PopulationProfessionCatalogItem> Professions { get; }
    }
}
