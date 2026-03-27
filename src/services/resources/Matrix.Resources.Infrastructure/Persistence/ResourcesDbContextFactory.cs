using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public sealed class ResourcesDbContextFactory : IDesignTimeDbContextFactory<ResourcesDbContext>
    {
        public ResourcesDbContext CreateDbContext(string[] args)
        {
            string environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            string basePath = ResolveStartupProjectPath();

            IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(basePath)
               .AddJsonFile(
                    path: "appsettings.json",
                    optional: true)
               .AddJsonFile(
                    path: $"appsettings.{environment}.json",
                    optional: true)
               .AddEnvironmentVariables()
               .Build();

            string connectionString = configuration.GetConnectionString("ResourcesDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'ResourcesDb' was not found.");

            var optionsBuilder = new DbContextOptionsBuilder<ResourcesDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ResourcesDbContext(optionsBuilder.Options);
        }

        private static string ResolveStartupProjectPath()
        {
            string current = Directory.GetCurrentDirectory();

            string fromSolutionRoot = Path.Combine(
                current,
                "src",
                "services",
                "resources",
                "Matrix.Resources.Api");

            if (Directory.Exists(fromSolutionRoot))
                return fromSolutionRoot;

            string fromInfrastructureProject = Path.GetFullPath(
                Path.Combine(
                    path1: current,
                    path2: "..",
                    path3: "Matrix.Resources.Api"));

            if (Directory.Exists(fromInfrastructureProject))
                return fromInfrastructureProject;

            throw new DirectoryNotFoundException(
                "Could not resolve path to Matrix.Resources.Api (startup project) for reading appsettings.");
        }
    }
}
