using Matrix.Education.Domain.Simulation;
using Matrix.Education.Infrastructure.Persistence.Repositories;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence;

public sealed class EducationSimulationRuntimeRepositoryTests
{
    private static SimulationRuntimeKey Runtime(string scenario) => new(new SimulationScenarioKey(scenario), new SimulationHostTypeKey("city"));

    [Fact]
    public async Task Ensure_PersistsIndependentHostsAndRejectsRebinding()
    {
        await using var db = EducationInfrastructureTestSupport.CreateDbContext();
        var repository = new EducationSimulationRuntimeRepository(db);
        var first = new SimulationHostId(Guid.NewGuid());
        var second = new SimulationHostId(Guid.NewGuid());
        await repository.EnsureAsync(first, Runtime("classic-city"));
        await repository.EnsureAsync(first, Runtime("classic-city"));
        await repository.EnsureAsync(second, Runtime("test-scenario"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(Runtime("classic-city"), await repository.GetAsync(first));
        Assert.Equal(Runtime("test-scenario"), await repository.GetAsync(second));
        Assert.Equal(2, db.SimulationRuntimes.Count());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.EnsureAsync(first, Runtime("test-scenario")));
        await Assert.ThrowsAsync<ArgumentException>(() => repository.EnsureAsync(first, default));
    }

    [Fact]
    public async Task Delete_RemovesOnlyTargetRuntime()
    {
        await using var db = EducationInfrastructureTestSupport.CreateDbContext();
        var repository = new EducationSimulationRuntimeRepository(db);
        var first = new SimulationHostId(Guid.NewGuid());
        var second = new SimulationHostId(Guid.NewGuid());
        await repository.EnsureAsync(first, Runtime("classic-city"));
        await repository.EnsureAsync(second, Runtime("classic-city"));
        await db.SaveChangesAsync();

        await new EducationSimulationDeletionRepository(db).DeleteSimulationDataAsync(first);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Null(await repository.GetAsync(first));
        Assert.Equal(Runtime("classic-city"), await repository.GetAsync(second));
    }
}
