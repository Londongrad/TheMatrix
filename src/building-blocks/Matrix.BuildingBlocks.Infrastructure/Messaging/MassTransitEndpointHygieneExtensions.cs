using MassTransit;
using MassTransit.RabbitMqTransport;
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
               .Validate(
                    validation: options => options.UnusedQueueExpirationHours >= 0,
                    failureMessage: $"{MassTransitEndpointHygieneOptions.SectionName}:UnusedQueueExpirationHours must be greater than or equal to 0.")
               .ValidateOnStart();

            return services;
        }

        public static IBusRegistrationConfigurator AddRabbitMqEndpointHygiene(
            this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConfigureEndpointsCallback((context, _, cfg) =>
            {
                MassTransitEndpointHygieneOptions options =
                    context.GetRequiredService<IOptions<MassTransitEndpointHygieneOptions>>().Value;

                if (options.DiscardSkippedMessages)
                    cfg.DiscardSkippedMessages();

                if (options.UnusedQueueExpirationHours <= 0 ||
                    cfg is not IRabbitMqReceiveEndpointConfigurator rabbitMqEndpoint)
                    return;

                rabbitMqEndpoint.SetQueueArgument(
                    key: "x-expires",
                    value: checked((int)TimeSpan.FromHours(options.UnusedQueueExpirationHours).TotalMilliseconds));
            });

            return configurator;
        }
    }
}
