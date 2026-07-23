using Matrix.Education.Application.Abstractions;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Integration;

public sealed class EducationRoutinePolicyRegistry
{
    private readonly IReadOnlyDictionary<SimulationRuntimeKey, IEducationParticipationRoutinePolicy> _policies;

    public EducationRoutinePolicyRegistry(IEnumerable<IEducationParticipationRoutinePolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        var registered = policies.ToArray();
        if (registered.Any(policy => policy is null || policy.RuntimeKey.IsEmpty))
            throw new InvalidOperationException("Routine policies must declare their runtime.");
        if (registered.GroupBy(policy => policy.RuntimeKey).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Only one education routine policy is allowed per runtime.");
        _policies = registered.ToDictionary(policy => policy.RuntimeKey);
    }

    public IEducationParticipationRoutinePolicy Resolve(SimulationRuntimeKey runtimeKey)
    {
        if (runtimeKey.IsEmpty)
            throw new ArgumentException("An education runtime is required.", nameof(runtimeKey));
        return _policies.TryGetValue(runtimeKey, out var policy)
            ? policy
            : throw new NotSupportedException($"Education routines are not configured for runtime '{runtimeKey}'.");
    }
}
