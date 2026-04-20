using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Configurations.Security;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class GatewayCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddGatewayCore(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<FrontendSecurityOptions>()
               .Bind(configuration.GetSection(FrontendSecurityOptions.SectionName))
               .Validate(
                    validation: options => !options.EnforceCookieOriginProtection ||
                                           options.AllowedOrigins.Any(x => !string.IsNullOrWhiteSpace(x)),
                    failureMessage: $"{FrontendSecurityOptions.SectionName}:AllowedOrigins must contain at least one origin when cookie origin protection is enabled.")
               .ValidateOnStart();

            services
               .AddScoped<ICityOperationsDashboardService, CityOperationsDashboardService>()
               .AddScoped<ICityProvisioningService, CityProvisioningService>()
               .AddScoped<IClassicCitySetupSessionStore, RedisClassicCitySetupSessionStore>()
               .AddScoped<IClassicCitySetupSessionService, ClassicCitySetupSessionService>()
               .AddHostedService<ClassicCitySetupSessionRecoveryHostedService>()
               .AddGatewayControllers()
               .AddGatewayCors(configuration);

            return services;
        }

        private static IServiceCollection AddGatewayControllers(this IServiceCollection services)
        {
            services.AddControllers()
               .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                           .Where(kvp => kvp.Value?.Errors.Count > 0)
                           .ToDictionary(
                                keySelector: kvp => kvp.Key,
                                elementSelector: kvp => kvp.Value!.Errors
                                   .Select(e => e.ErrorMessage)
                                   .ToArray());

                        return ApiProblemDetailsFactory.CreateObjectResult(
                            context: context.HttpContext,
                            statusCode: StatusCodes.Status400BadRequest,
                            code: "Gateway.ValidationError",
                            message: "Validation failed.",
                            errors: errors);
                    };
                });

            return services;
        }

        private static IServiceCollection AddGatewayCors(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string[] allowedOrigins = configuration
               .GetSection(FrontendSecurityOptions.SectionName)
               .GetSection(nameof(FrontendSecurityOptions.AllowedOrigins))
               .Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: GatewayCorsDefaults.PolicyName,
                    configurePolicy: policy =>
                    {
                        policy
                           .WithOrigins(allowedOrigins)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                    });
            });

            return services;
        }
    }
}
