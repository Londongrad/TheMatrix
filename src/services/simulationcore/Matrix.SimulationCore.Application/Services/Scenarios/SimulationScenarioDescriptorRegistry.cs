using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Services.Scenarios.Abstractions;

namespace Matrix.SimulationCore.Application.Services.Scenarios
{
    public sealed class SimulationScenarioDescriptorRegistry
    {
        private readonly IReadOnlyDictionary<SimulationRuntimeKey, SimulationScenarioDescriptor> _descriptors;
        private readonly IReadOnlyList<SimulationScenarioDescriptor> _orderedDescriptors;

        public SimulationScenarioDescriptorRegistry(
            IEnumerable<ISimulationScenarioDescriptorContributor> contributors)
        {
            ArgumentNullException.ThrowIfNull(contributors);

            var descriptors = new Dictionary<SimulationRuntimeKey, SimulationScenarioDescriptor>();

            foreach (ISimulationScenarioDescriptorContributor contributor in contributors)
            {
                ArgumentNullException.ThrowIfNull(contributor.Descriptor);

                if (!descriptors.TryAdd(contributor.Descriptor.RuntimeKey, contributor.Descriptor))
                    throw new InvalidOperationException(
                        $"Simulation scenario descriptor '{contributor.Descriptor.RuntimeKey}' is registered more than once.");
            }

            _descriptors = descriptors;
            _orderedDescriptors = descriptors.Values
               .OrderBy(descriptor => descriptor.RuntimeKey.ScenarioKey.Value, StringComparer.Ordinal)
               .ThenBy(descriptor => descriptor.RuntimeKey.HostTypeKey.Value, StringComparer.Ordinal)
               .ToArray();
        }

        public int Count => _descriptors.Count;

        public IReadOnlyList<SimulationScenarioDescriptor> List()
        {
            return _orderedDescriptors;
        }

        public SimulationScenarioDescriptor Resolve(SimulationRuntimeKey runtimeKey)
        {
            if (runtimeKey.IsEmpty)
                throw new ArgumentException(
                    "A runtime key is required to resolve a scenario descriptor.",
                    nameof(runtimeKey));

            return _descriptors.TryGetValue(runtimeKey, out SimulationScenarioDescriptor? descriptor)
                ? descriptor
                : throw new KeyNotFoundException(
                    $"Simulation scenario descriptor '{runtimeKey}' is not registered.");
        }
    }
}
