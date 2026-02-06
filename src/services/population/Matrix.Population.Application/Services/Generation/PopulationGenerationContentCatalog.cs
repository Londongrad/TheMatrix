using System.Reflection;
using System.Text.Json;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services.Abstractions;

namespace Matrix.Population.Application.Services.Generation
{
    public sealed class PopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly IReadOnlyList<string> MaleFirstNamesInternal =
            LoadStringCatalogResource("MaleFirstNames.json");

        private static readonly IReadOnlyList<string> FemaleFirstNamesInternal =
            LoadStringCatalogResource("FemaleFirstNames.json");

        private static readonly IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnamesInternal =
            LoadFamilySurnamesCatalogResource("FamilySurnames.json");

        private static readonly IReadOnlyList<PopulationProfessionCatalogItem> ProfessionsInternal =
            LoadProfessionsCatalogResource("Professions.json");

        public IReadOnlyList<string> MaleFirstNames => MaleFirstNamesInternal;
        public IReadOnlyList<string> FemaleFirstNames => FemaleFirstNamesInternal;
        public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames => FamilySurnamesInternal;
        public IReadOnlyList<PopulationProfessionCatalogItem> Professions => ProfessionsInternal;

        private static IReadOnlyList<string> LoadStringCatalogResource(string fileName)
        {
            string[]? values = LoadResource<string[]>(fileName);

            if (values is null || values.Length == 0)
                throw new InvalidOperationException(
                    $"Population generation catalog '{fileName}' must contain at least one value.");

            string[] sanitized = values
               .Select(x => x?.Trim())
               .Where(x => !string.IsNullOrWhiteSpace(x))
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToArray()!;

            if (sanitized.Length == 0)
                throw new InvalidOperationException(
                    $"Population generation catalog '{fileName}' does not contain usable values.");

            return Array.AsReadOnly(sanitized);
        }

        private static IReadOnlyList<PopulationFamilySurnameCatalogItem> LoadFamilySurnamesCatalogResource(
            string fileName)
        {
            PopulationFamilySurnameCatalogItem[]? entries = LoadResource<PopulationFamilySurnameCatalogItem[]>(fileName);

            if (entries is null || entries.Length == 0)
                throw new InvalidOperationException(
                    $"Population generation surnames catalog '{fileName}' must contain at least one entry.");

            PopulationFamilySurnameCatalogItem[] sanitized = entries
               .Select(x => new PopulationFamilySurnameCatalogItem(
                    Masculine: x.Masculine.Trim(),
                    Feminine: x.Feminine.Trim()))
               .Where(x => !string.IsNullOrWhiteSpace(x.Masculine) && !string.IsNullOrWhiteSpace(x.Feminine))
               .Distinct()
               .ToArray();

            if (sanitized.Length == 0)
                throw new InvalidOperationException(
                    $"Population generation surnames catalog '{fileName}' does not contain usable entries.");

            return Array.AsReadOnly(sanitized);
        }

        private static IReadOnlyList<PopulationProfessionCatalogItem> LoadProfessionsCatalogResource(
            string fileName)
        {
            PopulationProfessionCatalogItem[]? entries = LoadResource<PopulationProfessionCatalogItem[]>(fileName);

            if (entries is null || entries.Length == 0)
                throw new InvalidOperationException(
                    $"Population generation professions catalog '{fileName}' must contain at least one entry.");

            PopulationProfessionCatalogItem[] sanitized = entries
               .Select(x => new PopulationProfessionCatalogItem(
                    Title: x.Title.Trim(),
                    Weight: x.Weight))
               .Where(x => !string.IsNullOrWhiteSpace(x.Title) && x.Weight > 0)
               .GroupBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
               .Select(g => g.First())
               .ToArray();

            if (sanitized.Length == 0)
                throw new InvalidOperationException(
                    $"Population generation professions catalog '{fileName}' does not contain usable entries.");

            return Array.AsReadOnly(sanitized);
        }

        private static T? LoadResource<T>(string fileName)
        {
            Assembly assembly = typeof(PopulationGenerationContentCatalog).Assembly;
            string resourceName =
                $"{typeof(PopulationGenerationContentCatalog).Namespace}.Content.{fileName}";

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                throw new InvalidOperationException(
                    $"Embedded population generation catalog resource '{resourceName}' was not found.");

            T? value = JsonSerializer.Deserialize<T>(stream, JsonOptions);
            if (value is null)
                throw new InvalidOperationException(
                    $"Embedded population generation catalog resource '{resourceName}' is empty or invalid.");

            return value;
        }
    }
}
