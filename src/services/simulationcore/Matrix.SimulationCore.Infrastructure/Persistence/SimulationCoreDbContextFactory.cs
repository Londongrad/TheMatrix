using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Matrix.SimulationCore.Infrastructure.Persistence
{
    public sealed class SimulationCoreDbContextFactory : IDesignTimeDbContextFactory<SimulationCoreDbContext>
    {
        public SimulationCoreDbContext CreateDbContext(string[] args)
        {
            string environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            string basePath = ResolveStartupProjectPath();

            IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(basePath)
               .AddJsonFile(
                    path: "appsettings.json",
                    optional: false)
               .AddJsonFile(
                    path: $"appsettings.{environment}.json",
                    optional: true)
               .AddEnvironmentVariables()
               .Build();

            string connectionString = configuration.GetConnectionString("SimulationCoreDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'SimulationCoreDb' was not found.");

            var optionsBuilder = new DbContextOptionsBuilder<SimulationCoreDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new SimulationCoreDbContext(optionsBuilder.Options);
        }

        private static string ResolveStartupProjectPath()
        {
            string current = Directory.GetCurrentDirectory();

            // If command is run from solution root
            string fromSolutionRoot = Path.Combine(
                current,
                "src",
                "services",
                "simulationcore",
                "Matrix.SimulationCore.Api");

            if (Directory.Exists(fromSolutionRoot))
                return fromSolutionRoot;

            // If command is run from Infrastructure project directory
            string fromInfrastructureProject = Path.GetFullPath(
                Path.Combine(
                    path1: current,
                    path2: "..",
                    path3: "Matrix.SimulationCore.Api"));

            if (Directory.Exists(fromInfrastructureProject))
                return fromInfrastructureProject;

            throw new DirectoryNotFoundException(
                "Could not resolve path to Matrix.SimulationCore.Api (startup project) for reading appsettings.");
        }
    }
}
