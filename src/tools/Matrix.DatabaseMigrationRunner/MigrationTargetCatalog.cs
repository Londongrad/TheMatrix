using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Persistence;

namespace Matrix.DatabaseMigrationRunner
{
    internal static class MigrationTargetCatalog
    {
        private static readonly IReadOnlyDictionary<string, MigrationTarget> Targets =
            new Dictionary<string, MigrationTarget>(StringComparer.OrdinalIgnoreCase)
            {
                ["identity"] = new(
                    Name: "identity",
                    ConnectionStringName: "IdentityDb",
                    ApplyAsync: (
                        connectionString,
                        logger,
                        environment,
                        cancellationToken) => MigrationRunnerExecutor.ApplyAsync<IdentityDbContext>(
                        connectionString: connectionString,
                        logger: logger,
                        environment: environment,
                        serviceName: "Identity",
                        cancellationToken: cancellationToken)),
                ["economy"] = new(
                    Name: "economy",
                    ConnectionStringName: "EconomyDb",
                    ApplyAsync: (
                        connectionString,
                        logger,
                        environment,
                        cancellationToken) => MigrationRunnerExecutor.ApplyAsync<EconomyDbContext>(
                        connectionString: connectionString,
                        logger: logger,
                        environment: environment,
                        serviceName: "Economy",
                        cancellationToken: cancellationToken)),
                ["population"] = new(
                    Name: "population",
                    ConnectionStringName: "PopulationDb",
                    ApplyAsync: (
                        connectionString,
                        logger,
                        environment,
                        cancellationToken) => MigrationRunnerExecutor.ApplyAsync<PopulationDbContext>(
                        connectionString: connectionString,
                        logger: logger,
                        environment: environment,
                        serviceName: "Population",
                        cancellationToken: cancellationToken)),
                ["resources"] = new(
                    Name: "resources",
                    ConnectionStringName: "ResourcesDb",
                    ApplyAsync: (
                        connectionString,
                        logger,
                        environment,
                        cancellationToken) => MigrationRunnerExecutor.ApplyAsync<ResourcesDbContext>(
                        connectionString: connectionString,
                        logger: logger,
                        environment: environment,
                        serviceName: "Resources",
                        cancellationToken: cancellationToken)),
                ["simulationcore"] = new(
                    Name: "simulationcore",
                    ConnectionStringName: "SimulationCoreDb",
                    ApplyAsync: (
                        connectionString,
                        logger,
                        environment,
                        cancellationToken) => MigrationRunnerExecutor.ApplyAsync<SimulationCoreDbContext>(
                        connectionString: connectionString,
                        logger: logger,
                        environment: environment,
                        serviceName: "SimulationCore",
                        cancellationToken: cancellationToken)),
                ["simulationsystems"] = new(
                    Name: "simulationsystems",
                    ConnectionStringName: "SimulationSystemsDb",
                    ApplyAsync: (
                        connectionString,
                        logger,
                        environment,
                        cancellationToken) => MigrationRunnerExecutor.ApplyAsync<SimulationSystemsDbContext>(
                        connectionString: connectionString,
                        logger: logger,
                        environment: environment,
                        serviceName: "SimulationSystems",
                        cancellationToken: cancellationToken))
            };

        public static IReadOnlyCollection<MigrationTarget> Resolve(string service)
        {
            if (string.Equals(
                    a: service,
                    b: "all",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return Targets.Values.ToArray();

            if (!Targets.TryGetValue(
                    key: service,
                    value: out MigrationTarget? target))
                throw new InvalidOperationException($"Unknown service '{service}'.");

            return [target];
        }
    }
}
