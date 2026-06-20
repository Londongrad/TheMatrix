namespace Matrix.SimulationCore.Application.UseCases.Scenarios.ListSimulationScenarios
{
    public sealed record SimulationScenarioDto(
        string ScenarioKey,
        string HostTypeKey,
        string DisplayName,
        string CurrentModelVersion,
        bool SupportsProvisioning,
        IReadOnlyList<string> Capabilities);
}
