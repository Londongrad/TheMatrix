using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Healthcare.Api.Configurations;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Integration.Consumers;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.Healthcare.Api.Tests.Configurations
{
    public sealed class ServicesConfigurationTests
    {
        [Fact]
        public void ConfigureApplicationServices_WhenHealthcareDbIsMissing_ThrowsInvalidOperationException()
        {
            WebApplicationBuilder builder = CreateBuilder(
                new ConfigurationBuilder()
                   .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:HealthcareDb"] = ""
                        })
                   .Build());

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                builder.ConfigureApplicationServices);

            Assert.Contains(
                expectedSubstring: "Connection string 'HealthcareDb' is not configured",
                actualString: exception.Message);
        }

        [Fact]
        public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersCompleteServiceComposition()
        {
            WebApplicationBuilder builder = CreateBuilder(BuildValidConfiguration());

            builder.ConfigureApplicationServices();

            using ServiceProvider provider = builder.Services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider scopedServices = scope.ServiceProvider;
            AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>()
               .Value;

            Assert.Equal(
                expected: JwtAuthenticationExtensions.InternalCompositeJwtScheme,
                actual: authentication.DefaultAuthenticateScheme);
            Assert.NotNull(scopedServices.GetRequiredService<IHttpContextAccessor>());
            Assert.NotNull(scopedServices.GetRequiredService<ICurrentUserContext>());
            Assert.NotNull(scopedServices.GetRequiredService<HealthcareDbContext>());
            Assert.NotNull(scopedServices.GetRequiredService<IPatientProfileRepository>());
            Assert.NotNull(scopedServices.GetRequiredService<IHealthcareSimulationDeletionRepository>());
            Assert.NotNull(scopedServices.GetRequiredService<IHealthcareUnitOfWork>());
            Assert.NotNull(scopedServices.GetRequiredService<IMediator>());
            Assert.NotNull(scopedServices.GetRequiredService<PopulationResidentFactsConsumer>());
            Assert.NotNull(scopedServices.GetRequiredService<SimulationDeletedConsumer>());
            Assert.Same(
                expected: TimeProvider.System,
                actual: provider.GetRequiredService<TimeProvider>());
        }

        private static IConfiguration BuildValidConfiguration()
        {
            Dictionary<string, string?> values = new()
            {
                ["ConnectionStrings:HealthcareDb"] =
                    "Host=localhost;Port=5432;Database=healthcare_tests;Username=postgres;Password=postgres",
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "healthcare-api",
                ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "healthcare-api",
                ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                ["InternalServiceJwt:LifetimeSeconds"] = "300",
                ["RabbitMq:Host"] = "rabbitmq.test",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:EndpointHygiene:DiscardSkippedMessages"] = "true",
                ["DatabaseStartup:Enabled"] = "false"
            };

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }

        private static WebApplicationBuilder CreateBuilder(IConfiguration configuration)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = "Development"
                });
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddConfiguration(configuration);
            return builder;
        }
    }
}
