using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Matrix.SimulationSystems.Infrastructure.Persistence
{
    public sealed class SimulationSystemsDbContextFactory : IDesignTimeDbContextFactory<SimulationSystemsDbContext>
    {
        public SimulationSystemsDbContext CreateDbContext(string[] args)
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

            string connectionString = configuration.GetConnectionString("SimulationSystemsDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'SimulationSystemsDb' was not found.");

            var optionsBuilder = new DbContextOptionsBuilder<SimulationSystemsDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new SimulationSystemsDbContext(optionsBuilder.Options);
        }

        private static string ResolveStartupProjectPath()
        {
            string current = Directory.GetCurrentDirectory();

            string fromSolutionRoot = Path.Combine(
                current,
                "src",
                "services",
                "simulationsystems",
                "Matrix.SimulationSystems.Api");

            if (Directory.Exists(fromSolutionRoot))
                return fromSolutionRoot;

            string fromInfrastructureProject = Path.GetFullPath(
                Path.Combine(
                    path1: current,
                    path2: "..",
                    path3: "Matrix.SimulationSystems.Api"));

            if (Directory.Exists(fromInfrastructureProject))
                return fromInfrastructureProject;

            throw new DirectoryNotFoundException(
                "Could not resolve path to Matrix.SimulationSystems.Api (startup project) for reading appsettings.");
        }
    }
}
