using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Api.Configurations;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Outbox;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.SimulationCore.Api.Tests.TestSupport.SimulationCoreApiTestSupport;

namespace Matrix.SimulationCore.Api.Tests.Configurations
{
    public sealed class ServicesConfigurationTests
    {
        [Fact]
        public void
            ConfigureApplicationServices_WhenSimulationCoreDbConnectionStringIsMissing_ThrowsInvalidOperationException()
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:SimulationCoreDb"] = "",
                        ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                        ["InternalUserContextJwt:Audience"] = "simulationcore-api",
                        ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                        ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                        ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                        ["InternalServiceJwt:Audience"] = "simulationcore-api",
                        ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                        ["InternalServiceJwt:LifetimeSeconds"] = "300"
                    })
               .Build();
            WebApplicationBuilder builder = CreateBuilder(configuration);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                builder.ConfigureApplicationServices);

            Assert.Contains(
                expectedSubstring: "Connection string 'SimulationCoreDb' is not configured",
                actualString: exception.Message);
        }

        [Fact]
        public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersApiInfrastructureAndTypedClients()
        {
            WebApplicationBuilder builder = CreateBuilder(BuildValidApiConfiguration());

            builder.ConfigureApplicationServices();

            using ServiceProvider provider = builder.Services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>()
               .Value;

            Assert.Equal(
                expected: JwtAuthenticationExtensions.InternalCompositeJwtScheme,
                actual: authentication.DefaultAuthenticateScheme);
            Assert.Equal(
                expected: JwtAuthenticationExtensions.InternalCompositeJwtScheme,
                actual: authentication.DefaultChallengeScheme);
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUserContext>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISimulationClockRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityPopulationBootstrapClient>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityEconomyBootstrapClient>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityRoadSegmentConditionsClient>());
            Assert.IsType<SimulationCoreOutboxWriter>(
                scope.ServiceProvider.GetRequiredService<ISimulationCoreOutboxWriter>());
            Assert.IsType<ClassicCityOutboxWriter>(
                scope.ServiceProvider.GetRequiredService<IClassicCityOutboxWriter>());
            Assert.Same(
                expected: TimeProvider.System,
                actual: provider.GetRequiredService<TimeProvider>());
        }
    }
}
