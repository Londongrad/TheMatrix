using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Scenarios
{
    public sealed class SimulationScenarioDescriptor
    {
        public SimulationScenarioDescriptor(
            SimulationRuntimeKey runtimeKey,
            string displayName,
            SimulationModelVersion currentModelVersion,
            bool supportsProvisioning,
            IEnumerable<string> capabilities)
        {
            if (runtimeKey.IsEmpty)
                throw new ArgumentException("A scenario descriptor requires a runtime key.", nameof(runtimeKey));

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A scenario descriptor requires a display name.", nameof(displayName));

            if (string.IsNullOrWhiteSpace(currentModelVersion.Value))
                throw new ArgumentException(
                    "A scenario descriptor requires a current model version.",
                    nameof(currentModelVersion));

            ArgumentNullException.ThrowIfNull(capabilities);

            string[] normalizedCapabilities = capabilities
               .Select(capability => capability?.Trim())
               .Where(capability => !string.IsNullOrEmpty(capability))
               .Cast<string>()
               .ToArray();

            if (normalizedCapabilities.Length == 0)
                throw new ArgumentException(
                    "A scenario descriptor requires at least one capability.",
                    nameof(capabilities));

            string? duplicateCapability = normalizedCapabilities
               .GroupBy(capability => capability, StringComparer.Ordinal)
               .FirstOrDefault(group => group.Skip(1).Any())
               ?.Key;

            if (duplicateCapability is not null)
                throw new ArgumentException(
                    $"Scenario capability '{duplicateCapability}' is registered more than once.",
                    nameof(capabilities));

            RuntimeKey = runtimeKey;
            DisplayName = displayName.Trim();
            CurrentModelVersion = currentModelVersion;
            SupportsProvisioning = supportsProvisioning;
            Capabilities = normalizedCapabilities;
        }

        public SimulationRuntimeKey RuntimeKey { get; }
        public string DisplayName { get; }
        public SimulationModelVersion CurrentModelVersion { get; }
        public bool SupportsProvisioning { get; }
        public IReadOnlyList<string> Capabilities { get; }
    }
}
