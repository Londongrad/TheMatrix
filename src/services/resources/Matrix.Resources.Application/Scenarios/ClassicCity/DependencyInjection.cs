using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Resources.Application.Scenarios.ClassicCity
{
    internal static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddSingleton<CityStockpilePolicy>();
            return services;
        }
    }
}
