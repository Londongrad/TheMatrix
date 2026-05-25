using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Persistence
{
    [Collection(CurrentDirectorySensitiveCollection.Name)]
    public sealed class SimulationSystemsDbContextFactoryTests
    {
        [Fact]
        public void CreateDbContext_WhenSolutionRootApiProjectExists_UsesConnectionStringFromAppSettings()
        {
            string root = CreateTemporaryDirectory();
            string apiDirectory = Path.Combine(
                root,
                "src",
                "services",
                "simulationsystems",
                "Matrix.SimulationSystems.Api");
            Directory.CreateDirectory(apiDirectory);
            File.WriteAllText(
                path: Path.Combine(
                    path1: apiDirectory,
                    path2: "appsettings.json"),
                contents: """
                          {
                            "ConnectionStrings": {
                              "SimulationSystemsDb": "Host=localhost;Database=simulationsystems_factory_test;Username=test;Password=test"
                            }
                          }
                          """);

            using var _ = TemporaryCurrentDirectory.Change(root);
            using var __ = TemporaryEnvironmentVariable.Set(
                name: "ASPNETCORE_ENVIRONMENT",
                value: "Production");
            var factory = new SimulationSystemsDbContextFactory();

            using SimulationSystemsDbContext dbContext = factory.CreateDbContext([]);

            Assert.Equal(
                expected: "Npgsql.EntityFrameworkCore.PostgreSQL",
                actual: dbContext.Database.ProviderName);
            Assert.Contains(
                expectedSubstring: "Database=simulationsystems_factory_test",
                actualString: dbContext.Database.GetConnectionString());
        }

        [Fact]
        public void CreateDbContext_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
        {
            string root = CreateTemporaryDirectory();
            string apiDirectory = Path.Combine(
                root,
                "src",
                "services",
                "simulationsystems",
                "Matrix.SimulationSystems.Api");
            Directory.CreateDirectory(apiDirectory);
            File.WriteAllText(
                path: Path.Combine(
                    path1: apiDirectory,
                    path2: "appsettings.json"),
                contents: "{}");

            using var _ = TemporaryCurrentDirectory.Change(root);
            using var __ = TemporaryEnvironmentVariable.Set(
                name: "ASPNETCORE_ENVIRONMENT",
                value: "Production");
            var factory = new SimulationSystemsDbContextFactory();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));

            Assert.Contains(
                expectedSubstring: "Connection string 'SimulationSystemsDb' was not found",
                actualString: exception.Message);
        }

        [Fact]
        public void CreateDbContext_WhenApiProjectCannotBeResolved_ThrowsDirectoryNotFoundException()
        {
            string root = CreateTemporaryDirectory();

            using var _ = TemporaryCurrentDirectory.Change(root);
            using var __ = TemporaryEnvironmentVariable.Set(
                name: "ASPNETCORE_ENVIRONMENT",
                value: "Production");
            var factory = new SimulationSystemsDbContextFactory();

            DirectoryNotFoundException exception =
                Assert.Throws<DirectoryNotFoundException>(() => factory.CreateDbContext([]));

            Assert.Contains(
                expectedSubstring: "Could not resolve path to Matrix.SimulationSystems.Api",
                actualString: exception.Message);
        }

        private static string CreateTemporaryDirectory()
        {
            string path = Path.Combine(
                path1: Path.GetTempPath(),
                path2: "Matrix.SimulationSystems.FactoryTests",
                path3: Guid.NewGuid()
                   .ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
