using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
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
    string connectionString = ResolveConnectionString(builder.Configuration, target, options);
    await target.ApplyAsync(connectionString, logger, builder.Environment, CancellationToken.None);
}

return;

static string ResolveConnectionString(IConfiguration configuration, MigrationTarget target, MigrationRunnerOptions options)
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

internal sealed record MigrationTarget(
    string Name,
    string ConnectionStringName,
    Func<string, ILogger, IHostEnvironment, CancellationToken, Task> ApplyAsync);

internal static class MigrationTargetCatalog
{
    private static readonly IReadOnlyDictionary<string, MigrationTarget> Targets =
        new Dictionary<string, MigrationTarget>(StringComparer.OrdinalIgnoreCase)
        {
            ["identity"] = new(
                Name: "identity",
                ConnectionStringName: "IdentityDb",
                ApplyAsync: (connectionString, logger, environment, cancellationToken) => MigrationRunnerExecutor.ApplyAsync<IdentityDbContext>(
                    connectionString,
                    logger,
                    environment,
                    serviceName: "Identity",
                    cancellationToken)),
            ["economy"] = new(
                Name: "economy",
                ConnectionStringName: "EconomyDb",
                ApplyAsync: (connectionString, logger, environment, cancellationToken) => MigrationRunnerExecutor.ApplyAsync<EconomyDbContext>(
                    connectionString,
                    logger,
                    environment,
                    serviceName: "Economy",
                    cancellationToken)),
            ["population"] = new(
                Name: "population",
                ConnectionStringName: "PopulationDb",
                ApplyAsync: (connectionString, logger, environment, cancellationToken) => MigrationRunnerExecutor.ApplyAsync<PopulationDbContext>(
                    connectionString,
                    logger,
                    environment,
                    serviceName: "Population",
                    cancellationToken)),
            ["resources"] = new(
                Name: "resources",
                ConnectionStringName: "ResourcesDb",
                ApplyAsync: (connectionString, logger, environment, cancellationToken) => MigrationRunnerExecutor.ApplyAsync<ResourcesDbContext>(
                    connectionString,
                    logger,
                    environment,
                    serviceName: "Resources",
                    cancellationToken)),
            ["simulationcore"] = new(
                Name: "simulationcore",
                ConnectionStringName: "SimulationCoreDb",
                ApplyAsync: (connectionString, logger, environment, cancellationToken) => MigrationRunnerExecutor.ApplyAsync<SimulationCoreDbContext>(
                    connectionString,
                    logger,
                    environment,
                    serviceName: "SimulationCore",
                    cancellationToken)),
            ["simulationsystems"] = new(
                Name: "simulationsystems",
                ConnectionStringName: "SimulationSystemsDb",
                ApplyAsync: (connectionString, logger, environment, cancellationToken) => MigrationRunnerExecutor.ApplyAsync<SimulationSystemsDbContext>(
                    connectionString,
                    logger,
                    environment,
                    serviceName: "SimulationSystems",
                    cancellationToken))
        };

    public static IReadOnlyCollection<MigrationTarget> Resolve(string service)
    {
        if (string.Equals(service, "all", StringComparison.OrdinalIgnoreCase))
            return Targets.Values.ToArray();

        if (!Targets.TryGetValue(service, out MigrationTarget? target))
            throw new InvalidOperationException($"Unknown service '{service}'.");

        return [target];
    }
}

internal static class MigrationRunnerExecutor
{
    public static async Task ApplyAsync<TDbContext>(
        string connectionString,
        ILogger logger,
        IHostEnvironment environment,
        string serviceName,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        DbContextOptions<TDbContext> dbContextOptions = BuildDbContextOptions<TDbContext>(connectionString, environment);
        await using TDbContext dbContext = CreateDbContext<TDbContext>(dbContextOptions);

        await DatabaseMigrationExecutor.ApplyMigrationsAsync(
            dbContext,
            logger,
            serviceName,
            cancellationToken);
    }

    private static DbContextOptions<TDbContext> BuildDbContextOptions<TDbContext>(
        string connectionString,
        IHostEnvironment environment)
        where TDbContext : DbContext
    {
        DbContextOptionsBuilder<TDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        if (environment.IsDevelopment())
            optionsBuilder.EnableDetailedErrors();

        return optionsBuilder.Options;
    }

    private static TDbContext CreateDbContext<TDbContext>(DbContextOptions<TDbContext> options)
        where TDbContext : DbContext
    {
        object? dbContext = Activator.CreateInstance(typeof(TDbContext), options);

        return dbContext as TDbContext
               ?? throw new InvalidOperationException($"Failed to construct DbContext {typeof(TDbContext).Name}.");
    }
}

internal sealed class MigrationRunnerOptions
{
    public required string Service { get; init; }

    public string? Connection { get; init; }

    public bool ShowHelp { get; init; }

    public static MigrationRunnerOptions Parse(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)))
        {
            return new MigrationRunnerOptions
            {
                Service = "all",
                ShowHelp = true
            };
        }

        string? service = null;
        string? connection = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--service":
                    service = RequireValue(args, ++i, "--service");
                    break;
                case "--connection":
                    connection = RequireValue(args, ++i, "--connection");
                    break;
                default:
                    throw new InvalidOperationException($"Unknown argument '{args[i]}'. Use --help to see supported options.");
            }
        }

        if (string.IsNullOrWhiteSpace(service))
            throw new InvalidOperationException("Missing required argument --service.");

        return new MigrationRunnerOptions
        {
            Service = service,
            Connection = connection,
            ShowHelp = false
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("Matrix.DatabaseMigrationRunner");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/tools/Matrix.DatabaseMigrationRunner -- --service <identity|economy|population|resources|simulationcore|simulationsystems|all> [--connection <connection-string>]");
        Console.WriteLine();
        Console.WriteLine("Connection strings are read from configuration or environment variables like ConnectionStrings__IdentityDb.");
        Console.WriteLine("--connection is supported only for a single --service value.");
    }

    private static string RequireValue(string[] args, int index, string optionName)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            throw new InvalidOperationException($"Missing value for {optionName}.");

        return args[index];
    }
}
