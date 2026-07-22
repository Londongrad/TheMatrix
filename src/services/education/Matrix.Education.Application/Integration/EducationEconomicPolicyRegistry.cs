using Matrix.Education.Application.Abstractions;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Integration;

public sealed class EducationEconomicPolicyRegistry
{
    private readonly IReadOnlyDictionary<SimulationRuntimeKey, IEducationParticipationEconomicPolicy> _policies;

    public EducationEconomicPolicyRegistry(IEnumerable<IEducationParticipationEconomicPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        var registered = policies.ToArray();
        if (registered.Any(policy => policy is null || policy.RuntimeKey.IsEmpty))
            throw new InvalidOperationException("Economic policies must declare their runtime.");
        if (registered.GroupBy(policy => policy.RuntimeKey).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Only one education economic policy is allowed per runtime.");
        _policies = registered.ToDictionary(policy => policy.RuntimeKey);
    }

    public IEducationParticipationEconomicPolicy Resolve(SimulationRuntimeKey runtimeKey)
    {
        if (runtimeKey.IsEmpty)
            throw new ArgumentException("An education runtime is required.", nameof(runtimeKey));
        return _policies.TryGetValue(runtimeKey, out var policy)
            ? policy
            : throw new NotSupportedException($"Education economics are not configured for runtime '{runtimeKey}'.");
    }
}
