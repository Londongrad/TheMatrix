namespace Matrix.SimulationCore.Contracts.Scenarios.Catalog.Views
{
    public sealed record SimulationScenarioView(
        string ScenarioKey,
        string HostTypeKey,
        string DisplayName,
        string CurrentModelVersion,
        bool SupportsProvisioning,
        IReadOnlyList<string> Capabilities);
}
