using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Matrix.Education.Infrastructure.Persistence
{
    public sealed class EducationDbContextFactory : IDesignTimeDbContextFactory<EducationDbContext>
    {
        public EducationDbContext CreateDbContext(string[] args)
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

            string connectionString = configuration.GetConnectionString("EducationDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'EducationDb' was not found.");
            var options = new DbContextOptionsBuilder<EducationDbContext>();
            options.UseNpgsql(connectionString);

            return new EducationDbContext(options.Options);
        }

        private static string ResolveStartupProjectPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string fromSolutionRoot = Path.Combine(
                currentDirectory,
                "src",
                "services",
                "education",
                "Matrix.Education.Api");

            if (Directory.Exists(fromSolutionRoot))
                return fromSolutionRoot;

            string fromInfrastructureProject = Path.GetFullPath(
                Path.Combine(
                    currentDirectory,
                    "..",
                    "Matrix.Education.Api"));

            if (Directory.Exists(fromInfrastructureProject))
                return fromInfrastructureProject;

            throw new DirectoryNotFoundException(
                "Could not resolve path to Matrix.Education.Api for reading appsettings.");
        }
    }
}
