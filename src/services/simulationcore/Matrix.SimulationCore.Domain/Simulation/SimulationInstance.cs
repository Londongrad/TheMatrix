using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Errors;

namespace Matrix.SimulationCore.Domain.Simulation;

public sealed class SimulationInstance : AggregateRoot<SimulationId>
{
    private SimulationInstance(
        SimulationId id,
        SimulationHostId hostId,
        SimulationScenarioKey scenarioKey,
        SimulationHostTypeKey hostTypeKey,
        SimulationSeed seed,
        Guid runId,
        SimulationModelVersion modelVersion,
        Guid? provisioningCorrelationId,
        SimulationHostState state,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? archivedAtUtc)
        : base(id)
    {
        HostId = hostId;
        ScenarioKey = scenarioKey;
        HostTypeKey = hostTypeKey;
        Seed = seed;
        RunId = runId;
        ModelVersion = modelVersion;
        ProvisioningCorrelationId = provisioningCorrelationId;
        State = state;
        CreatedAtUtc = createdAtUtc;
        ArchivedAtUtc = archivedAtUtc;
    }

    private SimulationInstance()
        : base(default(SimulationId)) { }

    public SimulationHostId HostId { get; }
    public SimulationScenarioKey ScenarioKey { get; }
    public SimulationHostTypeKey HostTypeKey { get; }
    public SimulationSeed Seed { get; }
    public Guid RunId { get; }
    public SimulationModelVersion ModelVersion { get; }
    public Guid? ProvisioningCorrelationId { get; }
    public SimulationHostState State { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public SimulationRuntimeKey RuntimeKey => new(ScenarioKey, HostTypeKey);
    public bool IsActive => State == SimulationHostState.Active;
    public bool IsArchived => State == SimulationHostState.Archived;

    public static SimulationInstance Create(
        SimulationId id,
        SimulationHostId hostId,
        SimulationRuntimeKey runtimeKey,
        SimulationSeed seed,
        Guid runId,
        SimulationModelVersion modelVersion,
        Guid? provisioningCorrelationId,
        SimulationHostState initialState,
        DateTimeOffset createdAtUtc)
    {
        if (runtimeKey.IsEmpty)
            throw DomainErrorsFactory.SimulationRuntimeKeyMissing(nameof(runtimeKey));

        GuardHelper.AgainstEmptyGuid(
            id: id.Value,
            propertyName: nameof(id));
        GuardHelper.AgainstEmptyGuid(
            id: hostId.Value,
            propertyName: nameof(hostId));
        GuardHelper.AgainstEmptyGuid(
            id: runId,
            propertyName: nameof(runId));

        if (provisioningCorrelationId.HasValue)
            GuardHelper.AgainstEmptyGuid(
                id: provisioningCorrelationId.Value,
                propertyName: nameof(provisioningCorrelationId));

        if (string.IsNullOrEmpty(seed.Value))
            throw DomainErrorsFactory.SimulationSeedNullOrEmpty(nameof(seed));

        if (string.IsNullOrEmpty(modelVersion.Value))
            throw DomainErrorsFactory.SimulationModelVersionNullOrEmpty(nameof(modelVersion));

        GuardHelper.AgainstInvalidEnum(
            value: initialState,
            propertyName: nameof(initialState));

        if (initialState is not SimulationHostState.Active and not SimulationHostState.Provisioning)
            throw DomainErrorsFactory.SimulationInitialStateInvalid(
                value: initialState,
                propertyName: nameof(initialState));

        EnsureUtc(createdAtUtc);

        return new SimulationInstance(
            id: id,
            hostId: hostId,
            scenarioKey: runtimeKey.ScenarioKey,
            hostTypeKey: runtimeKey.HostTypeKey,
            seed: seed,
            runId: runId,
            modelVersion: modelVersion,
            provisioningCorrelationId: provisioningCorrelationId,
            state: initialState,
            createdAtUtc: createdAtUtc,
            archivedAtUtc: null);
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        EnsureUtc(archivedAtUtc);

        if (archivedAtUtc < CreatedAtUtc)
            throw DomainErrorsFactory.SimulationArchiveTimestampBeforeCreation(
                value: archivedAtUtc,
                propertyName: nameof(archivedAtUtc));

        if (IsArchived)
            return;

        State = SimulationHostState.Archived;
        ArchivedAtUtc = archivedAtUtc;
    }

    public void Activate()
    {
        if (State == SimulationHostState.Active)
            return;

        EnsureTransitionFrom(
            expectedState: SimulationHostState.Provisioning,
            targetState: SimulationHostState.Active);

        State = SimulationHostState.Active;
    }

    public void FailProvisioning()
    {
        if (State == SimulationHostState.ProvisioningFailed)
            return;

        EnsureTransitionFrom(
            expectedState: SimulationHostState.Provisioning,
            targetState: SimulationHostState.ProvisioningFailed);

        State = SimulationHostState.ProvisioningFailed;
    }

    public void RestartProvisioning()
    {
        if (State == SimulationHostState.Provisioning)
            return;

        EnsureTransitionFrom(
            expectedState: SimulationHostState.ProvisioningFailed,
            targetState: SimulationHostState.Provisioning);

        State = SimulationHostState.Provisioning;
    }

    private void EnsureTransitionFrom(
        SimulationHostState expectedState,
        SimulationHostState targetState)
    {
        if (State != expectedState)
            throw DomainErrorsFactory.SimulationStateTransitionInvalid(
                from: State,
                to: targetState,
                propertyName: nameof(State));
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        GuardHelper.Ensure(
            condition: value.Offset == TimeSpan.Zero,
            value: value,
            errorFactory: DomainErrorsFactory.SimulationTimestampMustBeUtc);
    }
}
