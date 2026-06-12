using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Resources.Application.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddSingleton<CityStockpilePolicy>();
            services.AddSingleton<CityStockpileBudgetGuard>();
            return services;
        }
    }
}
