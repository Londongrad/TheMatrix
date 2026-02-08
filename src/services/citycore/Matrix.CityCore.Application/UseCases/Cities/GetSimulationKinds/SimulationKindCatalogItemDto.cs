using Matrix.CityCore.Application.Services.Bootstrap;

namespace Matrix.CityCore.Application.UseCases.Cities.GetSimulationKinds
{
    public sealed record SimulationKindCatalogItemDto(
        string Kind,
        string DisplayName,
        string Description,
        bool SupportsAutomaticPopulationBootstrap,
        bool IsDefault)
    {
        public static SimulationKindCatalogItemDto FromDescriptor(SimulationKindDescriptor descriptor)
        {
            return new SimulationKindCatalogItemDto(
                Kind: descriptor.Kind.ToString(),
                DisplayName: descriptor.DisplayName,
                Description: descriptor.Description,
                SupportsAutomaticPopulationBootstrap: descriptor.SupportsAutomaticPopulationBootstrap,
                IsDefault: descriptor.IsDefault);
        }
    }
}
