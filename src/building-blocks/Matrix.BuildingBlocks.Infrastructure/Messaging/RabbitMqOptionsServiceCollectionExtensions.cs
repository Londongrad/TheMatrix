using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Infrastructure.Messaging
{
    public static class RabbitMqOptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<RabbitMqOptions>()
               .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
               .Validate(
                    validation: options => !string.IsNullOrWhiteSpace(options.Host),
                    failureMessage: "RabbitMq:Host is required.")
               .Validate(
                    validation: options => !string.IsNullOrWhiteSpace(options.Username),
                    failureMessage: "RabbitMq:Username is required.")
               .Validate(
                    validation: options => !string.IsNullOrWhiteSpace(options.Password),
                    failureMessage: "RabbitMq:Password is required.")
               .ValidateOnStart();

            return services;
        }
    }
}
