using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;

public sealed class SimulationInstanceRepositoryTests
{
    private static readonly SimulationRuntimeKey RuntimeKey = new(
        new SimulationScenarioKey("classic-city"),
        new SimulationHostTypeKey("city"));

    [Fact]
    public async Task AddAsync_ShouldPersistInstanceWithIndependentHostIdentity()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(AddAsync_ShouldPersistInstanceWithIndependentHostIdentity));
        var repository = new SimulationInstanceRepository(dbContext);
        SimulationInstance instance = CreateInstance();

        await repository.AddAsync(instance, CancellationToken.None);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        SimulationInstance? persisted = await repository.GetByIdAsync(
            simulationId: instance.Id,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.NotEqual(persisted.Id.Value, persisted.HostId.Value);
        Assert.Equal(instance.Id, persisted.Id);
        Assert.Equal(instance.HostId, persisted.HostId);
        Assert.Equal(RuntimeKey, persisted.RuntimeKey);
    }

    [Fact]
    public async Task GetByHostAsync_ShouldResolveRuntimeScopedHost()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(GetByHostAsync_ShouldResolveRuntimeScopedHost));
        var repository = new SimulationInstanceRepository(dbContext);
        SimulationInstance instance = CreateInstance();
        await dbContext.SimulationInstances.AddAsync(instance);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        SimulationInstance? persisted = await repository.GetByHostAsync(
            runtimeKey: RuntimeKey,
            hostId: instance.HostId,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(instance.Id, persisted.Id);
    }

    [Fact]
    public async Task DeleteByIdAsync_ShouldRemoveMatchingInstance()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(DeleteByIdAsync_ShouldRemoveMatchingInstance));
        var repository = new SimulationInstanceRepository(dbContext);
        SimulationInstance instance = CreateInstance();
        await dbContext.SimulationInstances.AddAsync(instance);
        await dbContext.SaveChangesAsync();

        await repository.DeleteByIdAsync(instance.Id, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.Null(await repository.GetByIdAsync(instance.Id, CancellationToken.None));
    }

    private static SimulationInstance CreateInstance()
    {
        return SimulationInstance.Create(
            id: SimulationId.New(),
            hostId: new SimulationHostId(Guid.NewGuid()),
            runtimeKey: RuntimeKey,
            seed: new SimulationSeed("seed-42"),
            runId: Guid.NewGuid(),
            modelVersion: new SimulationModelVersion("classic-city-v1"),
            provisioningCorrelationId: Guid.NewGuid(),
            initialState: SimulationHostState.Provisioning,
            createdAtUtc: new DateTimeOffset(
                year: 2042,
                month: 3,
                day: 4,
                hour: 5,
                minute: 6,
                second: 7,
                offset: TimeSpan.Zero));
    }
}
