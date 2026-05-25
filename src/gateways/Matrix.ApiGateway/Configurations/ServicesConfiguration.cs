using Matrix.ApiGateway.Configurations.DependencyInjection;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Logging;

namespace Matrix.ApiGateway.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            builder.AddSerilogLogging();

            builder.Services
               .AddOperationalHealthChecks(builder.Configuration)
               .AddTrustedForwardedHeaders(builder.Configuration)
               .AddGatewayCore(
                    configuration: builder.Configuration,
                    environment: builder.Environment)
               .AddGatewayAuth(builder.Configuration)
               .AddDownstreamServices(builder.Configuration)
               .AddBffFeatures(builder.Configuration);
        }
    }
}
