using Microsoft.Extensions.Configuration;
using Xunit;

namespace Matrix.DatabaseMigrationRunner.Tests
{
    public sealed class MigrationRunnerTests
    {
        [Fact]
        public void MigrationRunnerOptions_Parse_WhenHelpRequested_ReturnsHelpOptions()
        {
            var options = MigrationRunnerOptions.Parse(["--help"]);

            Assert.True(options.ShowHelp);
            Assert.Equal(
                expected: "all",
                actual: options.Service);
        }

        [Fact]
        public void MigrationRunnerOptions_Parse_WhenValuesProvided_ReturnsParsedOptions()
        {
            var options = MigrationRunnerOptions.Parse(
            [
                "--service",
                "identity",
                "--connection",
                "Host=db;Database=identity"
            ]);

            Assert.False(options.ShowHelp);
            Assert.Equal(
                expected: "identity",
                actual: options.Service);
            Assert.Equal(
                expected: "Host=db;Database=identity",
                actual: options.Connection);
        }

        [Fact]
        public void MigrationRunnerOptions_Parse_WhenServiceIsMissing_Throws()
        {
            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => MigrationRunnerOptions.Parse([]));

            Assert.Contains(
                expectedSubstring: "Missing required argument --service.",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public void ConnectionStringResolver_WhenSingleServiceConnectionOverrideIsProvided_ReturnsOverride()
        {
            string resolved = ConnectionStringResolver.Resolve(
                configuration: new ConfigurationBuilder().Build(),
                target: new MigrationTarget(
                    Name: "identity",
                    ConnectionStringName: "IdentityDb",
                    ApplyAsync: (
                        _,
                        _,
                        _,
                        _) => Task.CompletedTask),
                options: new MigrationRunnerOptions
                {
                    Service = "identity",
                    Connection = "Host=custom;Database=identity"
                });

            Assert.Equal(
                expected: "Host=custom;Database=identity",
                actual: resolved);
        }

        [Fact]
        public void ConnectionStringResolver_WhenOverrideTargetsAllServices_Throws()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => ConnectionStringResolver.Resolve(
                    configuration: new ConfigurationBuilder().Build(),
                    target: new MigrationTarget(
                        Name: "identity",
                        ConnectionStringName: "IdentityDb",
                        ApplyAsync: (
                            _,
                            _,
                            _,
                            _) => Task.CompletedTask),
                    options: new MigrationRunnerOptions
                    {
                        Service = "all",
                        Connection = "Host=custom;Database=identity"
                    }));

            Assert.Contains(
                expectedSubstring: "--connection can only be used with a single --service value.",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public void ConnectionStringResolver_WhenConfiguredConnectionStringExists_ReturnsIt()
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:IdentityDb"] = "Host=config;Database=identity"
                    })
               .Build();

            string resolved = ConnectionStringResolver.Resolve(
                configuration: configuration,
                target: new MigrationTarget(
                    Name: "identity",
                    ConnectionStringName: "IdentityDb",
                    ApplyAsync: (
                        _,
                        _,
                        _,
                        _) => Task.CompletedTask),
                options: new MigrationRunnerOptions
                {
                    Service = "identity"
                });

            Assert.Equal(
                expected: "Host=config;Database=identity",
                actual: resolved);
        }

        [Fact]
        public void ConnectionStringResolver_WhenConnectionStringIsMissing_Throws()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => ConnectionStringResolver.Resolve(
                    configuration: new ConfigurationBuilder().Build(),
                    target: new MigrationTarget(
                        Name: "identity",
                        ConnectionStringName: "IdentityDb",
                        ApplyAsync: (
                            _,
                            _,
                            _,
                            _) => Task.CompletedTask),
                    options: new MigrationRunnerOptions
                    {
                        Service = "identity"
                    }));

            Assert.Contains(
                expectedSubstring: "Connection string 'IdentityDb' is not configured.",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public void MigrationTargetCatalog_WhenAllIsRequested_ReturnsAllKnownTargets()
        {
            IReadOnlyCollection<MigrationTarget> targets = MigrationTargetCatalog.Resolve("all");

            Assert.Equal(
                expected: 6,
                actual: targets.Count);
            Assert.Contains(
                collection: targets,
                filter: x => x.Name == "identity");
            Assert.Contains(
                collection: targets,
                filter: x => x.Name == "simulationsystems");

            MigrationTarget identity = MigrationTargetCatalog.Resolve("identity")
               .Single();
            Assert.Equal(
                expected: "identity",
                actual: identity.Name);
            Assert.Equal(
                expected: "IdentityDb",
                actual: identity.ConnectionStringName);
            Assert.NotNull(identity.ApplyAsync);
        }

        [Fact]
        public void MigrationTargetCatalog_WhenUnknownServiceIsRequested_Throws()
        {
            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => MigrationTargetCatalog.Resolve("unknown"));

            Assert.Contains(
                expectedSubstring: "Unknown service 'unknown'.",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
