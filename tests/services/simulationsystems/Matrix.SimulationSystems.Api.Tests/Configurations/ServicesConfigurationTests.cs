using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Api.Configurations;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Configurations
{
    public sealed class ServicesConfigurationTests
    {
        [Fact]
        public void
            ConfigureApplicationServices_WhenSimulationSystemsDbConnectionStringIsMissing_ThrowsInvalidOperationException()
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:SimulationSystemsDb"] = "",
                        ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                        ["InternalUserContextJwt:Audience"] = "simulationsystems-api",
                        ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                        ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                        ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                        ["InternalServiceJwt:Audience"] = "simulationsystems-api",
                        ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                        ["InternalServiceJwt:LifetimeSeconds"] = "300"
                    })
               .Build();
            WebApplicationBuilder builder = CreateBuilder(configuration);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(builder.ConfigureApplicationServices);

            Assert.Contains(
                expectedSubstring: "Connection string 'SimulationSystemsDb' is not configured",
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
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityEnvironmentalConditionRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityBudgetAuthorizationClient>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityMapTopologyClient>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationalTripDispatcher>());
            Assert.Same(
                expected: TimeProvider.System,
                actual: provider.GetRequiredService<TimeProvider>());
        }
    }
}
