using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Matrix.DatabaseMigrationRunner;

internal static class MigrationRunnerApplication
{
    public static async Task RunAsync(
        HostApplicationBuilder builder,
        string[] args,
        CancellationToken cancellationToken)
    {
        MigrationRunnerOptions options = MigrationRunnerOptions.Parse(args);

        if (options.ShowHelp)
        {
            MigrationRunnerOptions.PrintHelp();
            return;
        }

        using IHost host = builder.Build();
        ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Matrix.DatabaseMigrationRunner");
        IReadOnlyCollection<MigrationTarget> targets = MigrationTargetCatalog.Resolve(options.Service);

        foreach (MigrationTarget target in targets)
        {
            string connectionString = ConnectionStringResolver.Resolve(
                builder.Configuration,
                target,
                options);

            await target.ApplyAsync(
                connectionString,
                logger,
                builder.Environment,
                cancellationToken);
        }
    }
}
