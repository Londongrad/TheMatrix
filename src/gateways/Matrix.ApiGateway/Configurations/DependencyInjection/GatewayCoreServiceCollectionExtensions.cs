using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Configurations.Security;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.BuildingBlocks.Api.Errors;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class GatewayCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddGatewayCore(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddOptions<CityOperationsDashboardOptions>()
               .Bind(configuration.GetSection(CityOperationsDashboardOptions.SectionName))
               .Validate(
                    validation: options => options.PanelReadTimeoutSeconds > 0,
                    failureMessage:
                    $"{CityOperationsDashboardOptions.SectionName}:PanelReadTimeoutSeconds must be greater than 0.")
               .Validate(
                    validation: options => options.HealthProbeTimeoutSeconds > 0,
                    failureMessage:
                    $"{CityOperationsDashboardOptions.SectionName}:HealthProbeTimeoutSeconds must be greater than 0.")
               .Validate(
                    validation: options => options.MaxConcurrentCitySnapshotLoads > 0,
                    failureMessage:
                    $"{CityOperationsDashboardOptions.SectionName}:MaxConcurrentCitySnapshotLoads must be greater than 0.")
               .ValidateOnStart();

            services.AddOptions<FrontendSecurityOptions>()
               .Bind(configuration.GetSection(FrontendSecurityOptions.SectionName))
               .PostConfigure(options =>
                {
                    options.AllowedOrigins = GetAllowedOrigins(
                        configuredOrigins: options.AllowedOrigins,
                        environment: environment);
                })
               .Validate(
                    validation: options => !options.EnforceCookieOriginProtection ||
                                           options.AllowedOrigins.Any(x => !string.IsNullOrWhiteSpace(x)),
                    failureMessage:
                    $"{FrontendSecurityOptions.SectionName}:AllowedOrigins must contain at least one origin when cookie origin protection is enabled.")
               .ValidateOnStart();

            services
               .AddScoped<ICityOperationsDashboardHealthProbe, CityOperationsDashboardHealthProbe>()
               .AddScoped<ICityOperationsDashboardSnapshotLoader, CityOperationsDashboardSnapshotLoader>()
               .AddScoped<ICityOperationsDashboardAlertBuilder, CityOperationsDashboardAlertBuilder>()
               .AddScoped<ICityOperationsDashboardRecentEventsBuilder, CityOperationsDashboardRecentEventsBuilder>()
               .AddScoped<ICityOperationsDashboardService, CityOperationsDashboardService>()
               .AddScoped<ICityProvisioningService, CityProvisioningService>()
               .AddScoped<IClassicCitySetupSessionStore, RedisClassicCitySetupSessionStore>()
               .AddScoped<IClassicCitySetupSessionService, ClassicCitySetupSessionService>()
               .AddHostedService<ClassicCitySetupSessionRecoveryHostedService>()
               .AddGatewayControllers()
               .AddGatewayCors(
                    configuration: configuration,
                    environment: environment);

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
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            string[] allowedOrigins = GetAllowedOrigins(
                configuredOrigins: configuration
                   .GetSection(FrontendSecurityOptions.SectionName)
                   .GetSection(nameof(FrontendSecurityOptions.AllowedOrigins))
                   .Get<string[]>() ?? [],
                environment: environment);

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

        private static string[] GetAllowedOrigins(
            IEnumerable<string> configuredOrigins,
            IHostEnvironment environment)
        {
            IEnumerable<string> allowedOrigins = configuredOrigins;
            if (environment.IsDevelopment())
                allowedOrigins = allowedOrigins.Concat(FrontendSecurityOptions.DevelopmentLocalAllowedOrigins);

            return allowedOrigins
               .Where(x => !string.IsNullOrWhiteSpace(x))
               .Select(x => x.Trim())
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToArray();
        }
    }
}
