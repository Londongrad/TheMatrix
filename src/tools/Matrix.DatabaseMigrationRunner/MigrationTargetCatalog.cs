using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Persistence;

namespace Matrix.DatabaseMigrationRunner;

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
