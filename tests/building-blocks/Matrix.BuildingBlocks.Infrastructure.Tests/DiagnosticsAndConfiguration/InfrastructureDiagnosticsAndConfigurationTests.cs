using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.BuildingBlocks.Infrastructure.Diagnostics;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.BuildingBlocks.Infrastructure.Tests.DiagnosticsAndConfiguration
{
    public sealed class InfrastructureDiagnosticsAndConfigurationTests
    {
        [Fact]
        public void TransientInfrastructureFailureDetector_WhenKnownTransientExceptionExists_ReturnsTrue()
        {
            Exception exception = new InvalidOperationException(
                message: "wrapper",
                innerException: new TimeoutException("temporary failure"));

            Assert.True(TransientInfrastructureFailureDetector.IsTransient(exception));
            Assert.False(
                TransientInfrastructureFailureDetector.IsTransient(new InvalidOperationException("permanent failure")));
        }

        [Fact]
        public void AddRabbitMqOptions_WhenConfigurationIsValid_BindsOptions()
        {
            ServiceCollection services = new();

            services.AddRabbitMqOptions(
                new ConfigurationBuilder()
                   .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["RabbitMq:Host"] = "rabbitmq.test",
                            ["RabbitMq:Port"] = "5673",
                            ["RabbitMq:VirtualHost"] = "/matrix",
                            ["RabbitMq:Username"] = "matrix-user",
                            ["RabbitMq:Password"] = "matrix-pass"
                        })
                   .Build());

            using ServiceProvider provider = services.BuildServiceProvider();
            RabbitMqOptions options = provider.GetRequiredService<IOptions<RabbitMqOptions>>()
               .Value;

            Assert.Equal(
                expected: "rabbitmq.test",
                actual: options.Host);
            Assert.Equal(
                expected: (ushort)5673,
                actual: options.Port);
            Assert.Equal(
                expected: "/matrix",
                actual: options.VirtualHost);
            Assert.Equal(
                expected: "matrix-user",
                actual: options.Username);
        }

        [Fact]
        public void AddRabbitMqOptions_WhenHostIsMissing_ThrowsOptionsValidationException()
        {
            ServiceCollection services = new();

            services.AddRabbitMqOptions(
                new ConfigurationBuilder()
                   .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["RabbitMq:Host"] = "",
                            ["RabbitMq:Username"] = "matrix-user",
                            ["RabbitMq:Password"] = "matrix-pass"
                        })
                   .Build());

            using ServiceProvider provider = services.BuildServiceProvider();

            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => provider
               .GetRequiredService<IOptions<RabbitMqOptions>>()
               .Value);

            Assert.Contains(
                expected: "RabbitMq:Host is required.",
                collection: exception.Failures);
        }

        [Fact]
        public void AddDatabaseStartup_WhenConfigured_BindsOptions()
        {
            ServiceCollection services = new();
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["DatabaseStartup:ApplyMigrationsOnStartup"] = "true",
                        ["DatabaseStartup:RunSeedOnStartup"] = "false"
                    })
               .Build();

            services.AddSingleton(configuration);
            services.AddDatabaseStartup(configuration);

            using ServiceProvider provider = services.BuildServiceProvider();
            DatabaseStartupOptions options = provider.GetRequiredService<IOptions<DatabaseStartupOptions>>()
               .Value;

            Assert.True(options.ApplyMigrationsOnStartup);
            Assert.False(options.RunSeedOnStartup);
        }

        [Fact]
        public async Task DatabaseStartupRunner_RunSeedIfEnabledAsync_UsesEnvironmentDefaultsForRunOrSkip()
        {
            int executed = 0;
            ServiceProvider developmentProvider = CreateDatabaseStartupProvider(
                new TestHostEnvironment
                {
                    EnvironmentName = Environments.Development
                });

            await DatabaseStartupRunner.RunSeedIfEnabledAsync(
                services: developmentProvider,
                serviceName: "identity",
                seedName: "permissions",
                seedAction: (
                    _,
                    _) =>
                {
                    executed++;
                    return Task.CompletedTask;
                });

            ServiceProvider productionProvider = CreateDatabaseStartupProvider(
                new TestHostEnvironment
                {
                    EnvironmentName = Environments.Production
                });

            await DatabaseStartupRunner.RunSeedIfEnabledAsync(
                services: productionProvider,
                serviceName: "identity",
                seedName: "permissions",
                seedAction: (
                    _,
                    _) =>
                {
                    executed += 100;
                    return Task.CompletedTask;
                });

            Assert.Equal(
                expected: 1,
                actual: executed);
        }

        [Fact]
        public async Task
            DatabaseStartupRunner_ApplyMigrationsIfEnabledAsync_WhenProductionAndUnset_SkipsWithoutDbContext()
        {
            using ServiceProvider provider = CreateDatabaseStartupProvider(
                new TestHostEnvironment
                {
                    EnvironmentName = Environments.Production
                });

            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<FakeDbContext>(
                services: provider,
                serviceName: "economy");
        }

        private static ServiceProvider CreateDatabaseStartupProvider(TestHostEnvironment environment)
        {
            ServiceCollection services = new();
            IConfiguration configuration = new ConfigurationBuilder().Build();
            services.AddSingleton(configuration);
            services.AddSingleton<IHostEnvironment>(environment);
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDatabaseStartup(configuration);
            return services.BuildServiceProvider();
        }

        private sealed class FakeDbContext : DbContext;
    }
}
