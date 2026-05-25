using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Matrix.DatabaseMigrationRunner
{
    internal static class MigrationRunnerApplication
    {
        public static async Task RunAsync(
            HostApplicationBuilder builder,
            string[] args,
            CancellationToken cancellationToken)
        {
            var options = MigrationRunnerOptions.Parse(args);

            if (options.ShowHelp)
            {
                MigrationRunnerOptions.PrintHelp();
                return;
            }

            using IHost host = builder.Build();
            ILogger logger = host.Services.GetRequiredService<ILoggerFactory>()
               .CreateLogger("Matrix.DatabaseMigrationRunner");
            IReadOnlyCollection<MigrationTarget> targets = MigrationTargetCatalog.Resolve(options.Service);

            foreach (MigrationTarget target in targets)
            {
                string connectionString = ConnectionStringResolver.Resolve(
                    configuration: builder.Configuration,
                    target: target,
                    options: options);

                await target.ApplyAsync(
                    arg1: connectionString,
                    arg2: logger,
                    arg3: builder.Environment,
                    arg4: cancellationToken);
            }
        }
    }
}
