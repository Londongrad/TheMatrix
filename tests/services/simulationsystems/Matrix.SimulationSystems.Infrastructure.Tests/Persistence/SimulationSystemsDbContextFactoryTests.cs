using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Persistence;

[Collection(CurrentDirectorySensitiveCollection.Name)]
public sealed class SimulationSystemsDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WhenSolutionRootApiProjectExists_UsesConnectionStringFromAppSettings()
    {
        string root = CreateTemporaryDirectory();
        string apiDirectory = Path.Combine(root, "src", "services", "simulationsystems", "Matrix.SimulationSystems.Api");
        Directory.CreateDirectory(apiDirectory);
        File.WriteAllText(
            Path.Combine(apiDirectory, "appsettings.json"),
            """
            {
              "ConnectionStrings": {
                "SimulationSystemsDb": "Host=localhost;Database=simulationsystems_factory_test;Username=test;Password=test"
              }
            }
            """);

        using var _ = TemporaryCurrentDirectory.Change(root);
        using var __ = TemporaryEnvironmentVariable.Set("ASPNETCORE_ENVIRONMENT", "Production");
        var factory = new SimulationSystemsDbContextFactory();

        using SimulationSystemsDbContext dbContext = factory.CreateDbContext([]);

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", dbContext.Database.ProviderName);
        Assert.Contains("Database=simulationsystems_factory_test", dbContext.Database.GetConnectionString());
    }

    [Fact]
    public void CreateDbContext_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        string root = CreateTemporaryDirectory();
        string apiDirectory = Path.Combine(root, "src", "services", "simulationsystems", "Matrix.SimulationSystems.Api");
        Directory.CreateDirectory(apiDirectory);
        File.WriteAllText(Path.Combine(apiDirectory, "appsettings.json"), "{}");

        using var _ = TemporaryCurrentDirectory.Change(root);
        using var __ = TemporaryEnvironmentVariable.Set("ASPNETCORE_ENVIRONMENT", "Production");
        var factory = new SimulationSystemsDbContextFactory();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));

        Assert.Contains("Connection string 'SimulationSystemsDb' was not found", exception.Message);
    }

    [Fact]
    public void CreateDbContext_WhenApiProjectCannotBeResolved_ThrowsDirectoryNotFoundException()
    {
        string root = CreateTemporaryDirectory();

        using var _ = TemporaryCurrentDirectory.Change(root);
        using var __ = TemporaryEnvironmentVariable.Set("ASPNETCORE_ENVIRONMENT", "Production");
        var factory = new SimulationSystemsDbContextFactory();

        DirectoryNotFoundException exception = Assert.Throws<DirectoryNotFoundException>(() => factory.CreateDbContext([]));

        Assert.Contains("Could not resolve path to Matrix.SimulationSystems.Api", exception.Message);
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "Matrix.SimulationSystems.FactoryTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
