using Microsoft.Extensions.Configuration;

namespace Matrix.DatabaseMigrationRunner;

internal static class ConnectionStringResolver
{
    public static string Resolve(
        IConfiguration configuration,
        MigrationTarget target,
        MigrationRunnerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Connection))
        {
            if (!string.Equals(target.Name, options.Service, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("--connection can only be used with a single --service value.");

            return options.Connection;
        }

        string? connectionString = configuration.GetConnectionString(target.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{target.ConnectionStringName}' is not configured. " +
                $"Provide ConnectionStrings__{target.ConnectionStringName} or use --connection for a single service.");
        }

        return connectionString;
    }
}
