using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Bootstrap
{
    public sealed record SimulationKindDescriptor(
        SimulationKind Kind,
        string DisplayName,
        string Description,
        bool SupportsAutomaticPopulationBootstrap,
        bool IsDefault = false);
}
