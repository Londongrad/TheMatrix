using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation;

public sealed class SimulationInstanceTests
{
    private static readonly SimulationRuntimeKey RuntimeKey = new(
        new SimulationScenarioKey("classic-city"),
        new SimulationHostTypeKey("city"));

    [Fact]
    public void Create_ShouldKeepSimulationAndHostIdentityIndependent()
    {
        SimulationId simulationId = SimulationId.New();
        var hostId = new SimulationHostId(Guid.NewGuid());
        Guid runId = Guid.NewGuid();
        Guid provisioningCorrelationId = Guid.NewGuid();
        DateTimeOffset createdAtUtc = UtcNow();

        SimulationInstance instance = SimulationInstance.Create(
            id: simulationId,
            hostId: hostId,
            runtimeKey: RuntimeKey,
            seed: new SimulationSeed("seed-42"),
            runId: runId,
            modelVersion: new SimulationModelVersion("classic-city-v1"),
            provisioningCorrelationId: provisioningCorrelationId,
            initialState: SimulationHostState.Provisioning,
            createdAtUtc: createdAtUtc);

        Assert.NotEqual(simulationId.Value, hostId.Value);
        Assert.Equal(simulationId, instance.Id);
        Assert.Equal(hostId, instance.HostId);
        Assert.Equal(RuntimeKey, instance.RuntimeKey);
        Assert.Equal("seed-42", instance.Seed.Value);
        Assert.Equal(runId, instance.RunId);
        Assert.Equal("classic-city-v1", instance.ModelVersion.Value);
        Assert.Equal(provisioningCorrelationId, instance.ProvisioningCorrelationId);
        Assert.Equal(SimulationHostState.Provisioning, instance.State);
        Assert.Equal(createdAtUtc, instance.CreatedAtUtc);
        Assert.Null(instance.ArchivedAtUtc);
        SimulationCreatedDomainEvent createdEvent =
            Assert.IsType<SimulationCreatedDomainEvent>(Assert.Single(instance.DomainEvents));
        Assert.Equal(simulationId, createdEvent.SimulationId);
        Assert.Equal(hostId, createdEvent.HostId);
        Assert.Equal(RuntimeKey, createdEvent.RuntimeKey);
    }

    [Fact]
    public void Create_ShouldAllowActiveInitialState()
    {
        SimulationInstance instance = Create(initialState: SimulationHostState.Active);

        Assert.True(instance.IsActive);
        Assert.False(instance.IsArchived);
    }

    [Theory]
    [InlineData(SimulationHostState.Archived)]
    [InlineData(SimulationHostState.ProvisioningFailed)]
    public void Create_ShouldRejectTerminalInitialState(SimulationHostState state)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Create(initialState: state));

        Assert.Equal("SimulationCore.Simulation.InitialState.Invalid", exception.Code);
    }

    [Fact]
    public void Create_ShouldRejectEmptyRuntimeKey()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            Create(runtimeKey: new SimulationRuntimeKey()));

        Assert.Equal("SimulationCore.Simulation.RuntimeKey.Missing", exception.Code);
    }

    [Fact]
    public void Create_ShouldRejectNonUtcTimestamp()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Create(
            createdAtUtc: new DateTimeOffset(
                year: 2042,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.FromHours(3))));

        Assert.Equal("SimulationCore.Simulation.Timestamp.NotUtc", exception.Code);
    }

    [Fact]
    public void Archive_ShouldMoveInstanceToArchivedState()
    {
        SimulationInstance instance = Create(initialState: SimulationHostState.Active);
        DateTimeOffset archivedAtUtc = UtcNow().AddHours(1);

        instance.Archive(archivedAtUtc);

        Assert.True(instance.IsArchived);
        Assert.False(instance.IsActive);
        Assert.Equal(archivedAtUtc, instance.ArchivedAtUtc);
        SimulationArchivedDomainEvent archivedEvent =
            Assert.IsType<SimulationArchivedDomainEvent>(instance.DomainEvents.Last());
        Assert.Equal(instance.Id, archivedEvent.SimulationId);
        Assert.Equal(instance.HostId, archivedEvent.HostId);
        Assert.Equal(RuntimeKey, archivedEvent.RuntimeKey);
    }

    [Fact]
    public void Archive_ShouldRejectTimestampBeforeCreation()
    {
        SimulationInstance instance = Create(initialState: SimulationHostState.Active);

        DomainException exception = Assert.Throws<DomainException>(() =>
            instance.Archive(UtcNow().AddTicks(-1)));

        Assert.Equal("SimulationCore.Simulation.ArchiveTimestamp.BeforeCreation", exception.Code);
    }

    [Fact]
    public void Activate_ShouldMoveProvisioningInstanceToActiveState()
    {
        SimulationInstance instance = Create();

        instance.Activate();

        Assert.True(instance.IsActive);
    }

    [Theory]
    [InlineData(SimulationHostState.ProvisioningFailed)]
    [InlineData(SimulationHostState.Archived)]
    public void Activate_ShouldRejectInvalidSourceState(SimulationHostState state)
    {
        SimulationInstance instance = CreateInState(state);

        DomainException exception = Assert.Throws<DomainException>(instance.Activate);

        Assert.Equal("SimulationCore.Simulation.StateTransition.Invalid", exception.Code);
    }

    [Fact]
    public void FailProvisioning_ShouldMoveProvisioningInstanceToFailedState()
    {
        SimulationInstance instance = Create();

        instance.FailProvisioning();

        Assert.Equal(SimulationHostState.ProvisioningFailed, instance.State);
    }

    [Theory]
    [InlineData(SimulationHostState.Active)]
    [InlineData(SimulationHostState.Archived)]
    public void FailProvisioning_ShouldRejectInvalidSourceState(SimulationHostState state)
    {
        SimulationInstance instance = CreateInState(state);

        DomainException exception = Assert.Throws<DomainException>(instance.FailProvisioning);

        Assert.Equal("SimulationCore.Simulation.StateTransition.Invalid", exception.Code);
    }

    [Fact]
    public void RestartProvisioning_ShouldMoveFailedInstanceToProvisioningState()
    {
        SimulationInstance instance = Create();
        instance.FailProvisioning();

        instance.RestartProvisioning();

        Assert.Equal(SimulationHostState.Provisioning, instance.State);
    }

    [Theory]
    [InlineData(SimulationHostState.Active)]
    [InlineData(SimulationHostState.Archived)]
    public void RestartProvisioning_ShouldRejectInvalidSourceState(SimulationHostState state)
    {
        SimulationInstance instance = CreateInState(state);

        DomainException exception = Assert.Throws<DomainException>(instance.RestartProvisioning);

        Assert.Equal("SimulationCore.Simulation.StateTransition.Invalid", exception.Code);
    }

    private static SimulationInstance Create(
        SimulationRuntimeKey? runtimeKey = null,
        SimulationHostState initialState = SimulationHostState.Provisioning,
        DateTimeOffset? createdAtUtc = null)
    {
        return SimulationInstance.Create(
            id: SimulationId.New(),
            hostId: new SimulationHostId(Guid.NewGuid()),
            runtimeKey: runtimeKey ?? RuntimeKey,
            seed: new SimulationSeed("seed-42"),
            runId: Guid.NewGuid(),
            modelVersion: new SimulationModelVersion("classic-city-v1"),
            provisioningCorrelationId: Guid.NewGuid(),
            initialState: initialState,
            createdAtUtc: createdAtUtc ?? UtcNow());
    }

    private static SimulationInstance CreateInState(SimulationHostState state)
    {
        SimulationInstance instance = Create(
            initialState: state == SimulationHostState.Archived
                ? SimulationHostState.Active
                : SimulationHostState.Provisioning);

        if (state == SimulationHostState.ProvisioningFailed)
            instance.FailProvisioning();
        else if (state == SimulationHostState.Archived)
            instance.Archive(UtcNow());
        else if (state == SimulationHostState.Active)
            instance.Activate();

        return instance;
    }

    private static DateTimeOffset UtcNow()
    {
        return new DateTimeOffset(
            year: 2042,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            offset: TimeSpan.Zero);
    }
}
