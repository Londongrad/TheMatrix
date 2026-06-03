namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities
{
    public static class ClassicCityCitiesServiceCollectionExtensions
    {
        public static IServiceCollection AddClassicCityCities(this IServiceCollection services)
        {
            services.AddScoped<ICityProvisioningService, CityProvisioningService>();
            return services;
        }
    }
}
