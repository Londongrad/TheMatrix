using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence;

public sealed class SimulationInstanceConfigurationTests
{
    [Fact]
    public void Model_ShouldMapSimulationInstanceAsIndependentRuntimeRoot()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(Model_ShouldMapSimulationInstanceAsIndependentRuntimeRoot));

        IEntityType entityType = dbContext.Model.FindEntityType(typeof(SimulationInstance))!;

        Assert.Equal("SimulationInstances", entityType.GetTableName());
        Assert.Equal(nameof(SimulationInstance.Id), entityType.FindPrimaryKey()!.Properties.Single().Name);
        Assert.DoesNotContain(
            entityType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType ==
                          typeof(Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.City));

        IIndex hostIdentityIndex = entityType.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
            [
                nameof(SimulationInstance.ScenarioKey),
                nameof(SimulationInstance.HostTypeKey),
                nameof(SimulationInstance.HostId)
            ]));

        Assert.True(hostIdentityIndex.IsUnique);
    }

    [Fact]
    public void Model_ShouldAttachClockToSimulationInstanceInsteadOfScenarioHost()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(Model_ShouldAttachClockToSimulationInstanceInsteadOfScenarioHost));

        IEntityType clockType = dbContext.Model.FindEntityType(typeof(SimulationClock))!;
        IForeignKey foreignKey = Assert.Single(clockType.GetForeignKeys());

        Assert.Equal(typeof(SimulationInstance), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(SimulationClock.Id), foreignKey.Properties.Single().Name);
    }
}
