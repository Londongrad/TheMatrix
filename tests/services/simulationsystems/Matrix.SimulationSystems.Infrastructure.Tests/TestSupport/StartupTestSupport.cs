using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Matrix.SimulationSystems.Infrastructure.Tests.TestSupport
{
    [CollectionDefinition(
        Name,
        DisableParallelization = true)]
    public sealed class CurrentDirectorySensitiveCollection : ICollectionFixture<CurrentDirectorySensitiveFixture>
    {
        public const string Name = "CurrentDirectorySensitive";
    }

    public sealed class CurrentDirectorySensitiveFixture { }

    internal static class StartupTestSupport
    {
        internal static IConfiguration BuildValidInfrastructureConfiguration()
        {
            Dictionary<string, string?> values = new()
            {
                ["ConnectionStrings:SimulationSystemsDb"] =
                    "Host=localhost;Database=simulationsystems_test;Username=test;Password=test",
                ["PostgresResilience:MaxRetryCount"] = "3",
                ["PostgresResilience:MaxRetryDelaySeconds"] = "2",
                ["RabbitMq:Host"] = "localhost",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["DownstreamServices:Economy"] = "https://economy.test",
                ["DownstreamServices:SimulationCore"] = "https://simulationcore.test"
            };

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }

        internal static IConfiguration BuildInternalServiceJwtConfiguration()
        {
            Dictionary<string, string?> values = new()
            {
                [$"{InternalServiceJwtOptions.SectionName}:Issuer"] = "internal-issuer",
                [$"{InternalServiceJwtOptions.SectionName}:Audience"] = "internal-audience",
                [$"{InternalServiceJwtOptions.SectionName}:SigningKey"] = "abcdefghijklmnopqrstuvwxyz123456",
                [$"{InternalServiceJwtOptions.SectionName}:LifetimeSeconds"] = "300"
            };

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }
    }

    internal sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Matrix.SimulationSystems.Infrastructure.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    internal sealed class TemporaryCurrentDirectory : IDisposable
    {
        private readonly string _previous = Directory.GetCurrentDirectory();

        private TemporaryCurrentDirectory() { }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_previous);
        }

        public static TemporaryCurrentDirectory Change(string path)
        {
            Directory.SetCurrentDirectory(path);
            return new TemporaryCurrentDirectory();
        }
    }

    internal sealed class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;

        private TemporaryEnvironmentVariable(string name)
        {
            _name = name;
            _previous = Environment.GetEnvironmentVariable(name);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(
                variable: _name,
                value: _previous);
        }

        public static TemporaryEnvironmentVariable Set(
            string name,
            string? value)
        {
            var scope = new TemporaryEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(
                variable: name,
                value: value);
            return scope;
        }
    }
}
