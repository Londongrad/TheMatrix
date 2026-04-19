using Matrix.DatabaseMigrationRunner;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
await MigrationRunnerApplication.RunAsync(builder, args, CancellationToken.None);
