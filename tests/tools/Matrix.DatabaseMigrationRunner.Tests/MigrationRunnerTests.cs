using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.DatabaseMigrationRunner.Tests;

public sealed class MigrationRunnerTests
{
    [Fact]
    public void MigrationRunnerOptions_Parse_WhenHelpRequested_ReturnsHelpOptions()
    {
        MigrationRunnerOptions options = MigrationRunnerOptions.Parse(["--help"]);

        Assert.True(options.ShowHelp);
        Assert.Equal("all", options.Service);
    }

    [Fact]
    public void MigrationRunnerOptions_Parse_WhenValuesProvided_ReturnsParsedOptions()
    {
        MigrationRunnerOptions options = MigrationRunnerOptions.Parse(
            ["--service", "identity", "--connection", "Host=db;Database=identity"]);

        Assert.False(options.ShowHelp);
        Assert.Equal("identity", options.Service);
        Assert.Equal("Host=db;Database=identity", options.Connection);
    }

    [Fact]
    public void MigrationRunnerOptions_Parse_WhenServiceIsMissing_Throws()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => MigrationRunnerOptions.Parse([]));

        Assert.Contains("Missing required argument --service.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionStringResolver_WhenSingleServiceConnectionOverrideIsProvided_ReturnsOverride()
    {
        string resolved = ConnectionStringResolver.Resolve(
            configuration: new ConfigurationBuilder().Build(),
            target: new MigrationTarget(
                Name: "identity",
                ConnectionStringName: "IdentityDb",
                ApplyAsync: (_, _, _, _) => Task.CompletedTask),
            options: new MigrationRunnerOptions
            {
                Service = "identity",
                Connection = "Host=custom;Database=identity"
            });

        Assert.Equal("Host=custom;Database=identity", resolved);
    }

    [Fact]
    public void ConnectionStringResolver_WhenOverrideTargetsAllServices_Throws()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ConnectionStringResolver.Resolve(
                configuration: new ConfigurationBuilder().Build(),
                target: new MigrationTarget(
                    Name: "identity",
                    ConnectionStringName: "IdentityDb",
                    ApplyAsync: (_, _, _, _) => Task.CompletedTask),
                options: new MigrationRunnerOptions
                {
                    Service = "all",
                    Connection = "Host=custom;Database=identity"
                }));

        Assert.Contains("--connection can only be used with a single --service value.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionStringResolver_WhenConfiguredConnectionStringExists_ReturnsIt()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDb"] = "Host=config;Database=identity"
            })
            .Build();

        string resolved = ConnectionStringResolver.Resolve(
            configuration: configuration,
            target: new MigrationTarget(
                Name: "identity",
                ConnectionStringName: "IdentityDb",
                ApplyAsync: (_, _, _, _) => Task.CompletedTask),
            options: new MigrationRunnerOptions
            {
                Service = "identity"
            });

        Assert.Equal("Host=config;Database=identity", resolved);
    }

    [Fact]
    public void ConnectionStringResolver_WhenConnectionStringIsMissing_Throws()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ConnectionStringResolver.Resolve(
                configuration: new ConfigurationBuilder().Build(),
                target: new MigrationTarget(
                    Name: "identity",
                    ConnectionStringName: "IdentityDb",
                    ApplyAsync: (_, _, _, _) => Task.CompletedTask),
                options: new MigrationRunnerOptions
                {
                    Service = "identity"
                }));

        Assert.Contains("Connection string 'IdentityDb' is not configured.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationTargetCatalog_WhenAllIsRequested_ReturnsAllKnownTargets()
    {
        IReadOnlyCollection<MigrationTarget> targets = MigrationTargetCatalog.Resolve("all");

        Assert.Equal(6, targets.Count);
        Assert.Contains(targets, x => x.Name == "identity");
        Assert.Contains(targets, x => x.Name == "simulationsystems");

        MigrationTarget identity = MigrationTargetCatalog.Resolve("identity").Single();
        Assert.Equal("identity", identity.Name);
        Assert.Equal("IdentityDb", identity.ConnectionStringName);
        Assert.NotNull(identity.ApplyAsync);
    }

    [Fact]
    public void MigrationTargetCatalog_WhenUnknownServiceIsRequested_Throws()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => MigrationTargetCatalog.Resolve("unknown"));

        Assert.Contains("Unknown service 'unknown'.", exception.Message, StringComparison.Ordinal);
    }
}
