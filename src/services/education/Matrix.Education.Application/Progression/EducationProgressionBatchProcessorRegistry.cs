using Matrix.Education.Application.Abstractions;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Progression
{
    public sealed class EducationProgressionBatchProcessorRegistry
    {
        private readonly IReadOnlyDictionary<SimulationRuntimeKey, IEducationProgressionBatchProcessor> _processors;

        public EducationProgressionBatchProcessorRegistry(
            IEnumerable<IEducationProgressionBatchProcessor> processors)
        {
            ArgumentNullException.ThrowIfNull(processors);

            IEducationProgressionBatchProcessor[] registered = processors.ToArray();
            IEducationProgressionBatchProcessor? invalid = registered
               .FirstOrDefault(processor => processor.RuntimeKey.IsEmpty);
            if (invalid is not null)
                throw new InvalidOperationException(
                    $"Education progression processor '{invalid.GetType().FullName}' has an empty runtime key.");

            IGrouping<SimulationRuntimeKey, IEducationProgressionBatchProcessor>? duplicate = registered
               .GroupBy(processor => processor.RuntimeKey)
               .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
                throw new InvalidOperationException(
                    $"Multiple education progression processors are registered for runtime '{duplicate.Key}'.");

            _processors = registered.ToDictionary(processor => processor.RuntimeKey);
        }

        public IEducationProgressionBatchProcessor Resolve(SimulationRuntimeKey runtimeKey)
        {
            if (runtimeKey.IsEmpty)
                throw new ArgumentException(
                    message: "An education progression runtime key is required.",
                    paramName: nameof(runtimeKey));

            return _processors.TryGetValue(runtimeKey, out IEducationProgressionBatchProcessor? processor)
                ? processor
                : throw new NotSupportedException(
                    $"Education progression is not configured for runtime '{runtimeKey}'.");
        }
    }
}
