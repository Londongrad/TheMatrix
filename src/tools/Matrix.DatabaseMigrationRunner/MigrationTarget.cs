using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Matrix.DatabaseMigrationRunner;

internal sealed record MigrationTarget(
    string Name,
    string ConnectionStringName,
    Func<string, ILogger, IHostEnvironment, CancellationToken, Task> ApplyAsync);
