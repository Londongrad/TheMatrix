using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Matrix.Healthcare.Infrastructure.Persistence
{
    public sealed class HealthcareDbContextFactory : IDesignTimeDbContextFactory<HealthcareDbContext>
    {
        public HealthcareDbContext CreateDbContext(string[] args)
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
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

            string connectionString = configuration.GetConnectionString("HealthcareDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'HealthcareDb' was not found.");
            var options = new DbContextOptionsBuilder<HealthcareDbContext>();
            options.UseNpgsql(connectionString);

            return new HealthcareDbContext(options.Options);
        }

        private static string ResolveStartupProjectPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string fromSolutionRoot = Path.Combine(
                currentDirectory,
                "src",
                "services",
                "healthcare",
                "Matrix.Healthcare.Api");

            if (Directory.Exists(fromSolutionRoot))
                return fromSolutionRoot;

            string fromInfrastructureProject = Path.GetFullPath(
                Path.Combine(
                    currentDirectory,
                    "..",
                    "Matrix.Healthcare.Api"));

            if (Directory.Exists(fromInfrastructureProject))
                return fromInfrastructureProject;

            throw new DirectoryNotFoundException(
                "Could not resolve path to Matrix.Healthcare.Api for reading appsettings.");
        }
    }
}
