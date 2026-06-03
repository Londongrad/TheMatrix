namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    public static class ClassicCityDashboardServiceCollectionExtensions
    {
        public static IServiceCollection AddClassicCityDashboard(
            this IServiceCollection services,
            IConfiguration configuration)
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

            services
               .AddScoped<ICityOperationsDashboardHealthProbe, CityOperationsDashboardHealthProbe>()
               .AddScoped<ICityOperationsDashboardSnapshotLoader, CityOperationsDashboardSnapshotLoader>()
               .AddScoped<ICityOperationsDashboardAlertBuilder, CityOperationsDashboardAlertBuilder>()
               .AddScoped<ICityOperationsDashboardRecentEventsBuilder, CityOperationsDashboardRecentEventsBuilder>()
               .AddScoped<ICityOperationsDashboardService, CityOperationsDashboardService>();

            return services;
        }
    }
}
