using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Matrix.Population.Infrastructure.Persistence
{
    public sealed class PopulationDbContextFactory
        : IDesignTimeDbContextFactory<PopulationDbContext>
    {
        public PopulationDbContext CreateDbContext(string[] args)
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

            string connectionString = configuration.GetConnectionString("PopulationDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'PopulationDb' was not found.");
            var options = new DbContextOptionsBuilder<PopulationDbContext>();
            options.UseNpgsql(connectionString);
            return new PopulationDbContext(options.Options);
        }

        private static string ResolveStartupProjectPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string fromSolutionRoot = Path.Combine(
                currentDirectory,
                "src",
                "services",
                "population",
                "Matrix.Population.Api");
            if (Directory.Exists(fromSolutionRoot))
                return fromSolutionRoot;

            string fromInfrastructureProject = Path.GetFullPath(
                Path.Combine(
                    currentDirectory,
                    "..",
                    "Matrix.Population.Api"));
            if (Directory.Exists(fromInfrastructureProject))
                return fromInfrastructureProject;

            throw new DirectoryNotFoundException(
                "Could not resolve path to Matrix.Population.Api for reading appsettings.");
        }
    }
}
