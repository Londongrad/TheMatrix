using Matrix.Population.Domain.Models;

namespace Matrix.Population.Domain.Services.Abstractions
{
    public interface IPopulationGenerationContentCatalog
    {
        IReadOnlyList<string> MaleFirstNames { get; }
        IReadOnlyList<string> FemaleFirstNames { get; }
        IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames { get; }
        IReadOnlyList<PopulationProfessionCatalogItem> Professions { get; }
    }
}
