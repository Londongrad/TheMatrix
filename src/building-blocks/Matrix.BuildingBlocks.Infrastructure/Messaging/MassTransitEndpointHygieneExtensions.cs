using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Matrix.BuildingBlocks.Infrastructure.Messaging
{
    public static class MassTransitEndpointHygieneExtensions
    {
        public static IServiceCollection AddMassTransitEndpointHygieneOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<MassTransitEndpointHygieneOptions>()
               .Bind(configuration.GetSection(MassTransitEndpointHygieneOptions.SectionName))
               .ValidateOnStart();

            return services;
        }

        public static IBusRegistrationConfigurator AddRabbitMqEndpointHygiene(
            this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConfigureEndpointsCallback((
                context,
                _,
                cfg) =>
            {
                MassTransitEndpointHygieneOptions options =
                    context.GetRequiredService<IOptions<MassTransitEndpointHygieneOptions>>()
                       .Value;

                if (options.DiscardSkippedMessages)
                    cfg.DiscardSkippedMessages();
            });

            return configurator;
        }
    }
}
