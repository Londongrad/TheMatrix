using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.Common;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    internal static class DownstreamHttpClientDefaults
    {
        public static void ConfigureServiceBaseAddress(
            IServiceProvider sp,
            HttpClient client,
            string serviceName)
        {
            DownstreamServicesOptions options = sp
               .GetRequiredService<IOptions<DownstreamServicesOptions>>()
               .Value;

            string baseAddress = serviceName switch
            {
                DownstreamServiceNames.SimulationCore => options.SimulationCore,
                DownstreamServiceNames.SimulationSystems => options.SimulationSystems,
                DownstreamServiceNames.Economy => options.Economy,
                DownstreamServiceNames.Resources => options.Resources,
                DownstreamServiceNames.Population => options.Population,
                DownstreamServiceNames.Education => options.Education,
                DownstreamServiceNames.Identity => options.Identity,
                _ => throw new InvalidOperationException($"Unsupported downstream service '{serviceName}'.")
            };

            client.BaseAddress = new Uri(
                uriString: baseAddress,
                uriKind: UriKind.Absolute);
        }

        public static void ConfigureTimeout(
            IServiceProvider sp,
            HttpClient client)
        {
            IHostEnvironment environment = sp.GetRequiredService<IHostEnvironment>();

            client.Timeout = environment.IsDevelopment()
                ? TimeSpan.FromMinutes(10)
                : TimeSpan.FromSeconds(20);
        }
    }
}
