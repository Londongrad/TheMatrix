using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Bootstrap
{
    public sealed record SimulationKindDescriptor(
        SimulationKind Kind,
        string DisplayName,
        string Description,
        bool SupportsAutomaticPopulationBootstrap,
        bool IsDefault = false);
}
