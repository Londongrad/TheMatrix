using Matrix.ApiGateway.Configurations.DependencyInjection;

namespace Matrix.ApiGateway.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            builder.Services
               .AddGatewayCore()
               .AddGatewayAuth(builder.Configuration)
               .AddDownstreamServices(builder.Configuration)
               .AddBffFeatures(builder.Configuration);
        }
    }
}
