using Matrix.ApiGateway.DownstreamClients.HttpHandlers;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    internal static class InternalDownstreamClientServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddInternalDownstreamClient<TClient, TImplementation>(
            this IServiceCollection services,
            string serviceName)
            where TClient : class
            where TImplementation : class, TClient
        {
            return services.AddHttpClient<TClient, TImplementation>((
                        sp,
                        client) =>
                    DownstreamHttpClientDefaults.ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: serviceName))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .AddDownstreamReadResilience(serviceName)
               .ConfigureHttpClient(DownstreamHttpClientDefaults.ConfigureTimeout);
        }
    }
}
