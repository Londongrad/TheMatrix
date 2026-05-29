using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;

public sealed class SimulationHostReadRepositoryTests
{
    private static readonly SimulationRuntimeKey RuntimeKey = new(
        new SimulationScenarioKey("metro"),
        new SimulationHostTypeKey("network"));

    [Fact]
    public async Task GetBySimulationIdAsync_WhenInstanceIsMissing_ReturnsNull()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(GetBySimulationIdAsync_WhenInstanceIsMissing_ReturnsNull));
        var repository = new SimulationHostReadRepository(dbContext);

        SimulationHost? result = await repository.GetBySimulationIdAsync(
            simulationId: new SimulationId(Guid.NewGuid()),
            cancellationToken: CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(SimulationHostState.Active)]
    [InlineData(SimulationHostState.Provisioning)]
    [InlineData(SimulationHostState.ProvisioningFailed)]
    [InlineData(SimulationHostState.Archived)]
    public async Task GetBySimulationIdAsync_ProjectsRuntimeInstance(SimulationHostState state)
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            $"{nameof(GetBySimulationIdAsync_ProjectsRuntimeInstance)}_{state}");
        SimulationInstance instance = CreateInstance(state);
        await dbContext.SimulationInstances.AddAsync(instance);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new SimulationHostReadRepository(dbContext);

        SimulationHost? result = await repository.GetBySimulationIdAsync(
            simulationId: instance.Id,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(
            expected: instance.Id,
            actual: result.SimulationId);
        Assert.Equal(
            expected: instance.HostId,
            actual: result.HostId);
        Assert.Equal(
            expected: RuntimeKey,
            actual: result.RuntimeKey);
        Assert.Equal(
            expected: state,
            actual: result.State);
        Assert.Equal(
            expected: instance.CreatedAtUtc,
            actual: result.CreatedAtUtc);
        Assert.Equal(
            expected: instance.ArchivedAtUtc,
            actual: result.ArchivedAtUtc);
        Assert.Empty(dbContext.ChangeTracker.Entries<SimulationInstance>());
    }

    private static SimulationInstance CreateInstance(SimulationHostState state)
    {
        DateTimeOffset createdAtUtc = RepositoryTestData.BaseUtc;
        SimulationHostState initialState = state is SimulationHostState.Provisioning or
            SimulationHostState.ProvisioningFailed
                ? SimulationHostState.Provisioning
                : SimulationHostState.Active;
        SimulationInstance instance = SimulationInstance.Create(
            id: new SimulationId(Guid.NewGuid()),
            hostId: new SimulationHostId(Guid.NewGuid()),
            runtimeKey: RuntimeKey,
            seed: new SimulationSeed("metro-seed"),
            runId: Guid.NewGuid(),
            modelVersion: new SimulationModelVersion("metro-v1"),
            provisioningCorrelationId: null,
            initialState: initialState,
            createdAtUtc: createdAtUtc);

        if (state == SimulationHostState.ProvisioningFailed)
            instance.FailProvisioning();

        if (state == SimulationHostState.Archived)
            instance.Archive(createdAtUtc.AddHours(1));

        return instance;
    }
}
