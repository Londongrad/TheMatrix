using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Application.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddScoped<CityEconomyRecurringCycleExecutionService>();

            return services;
        }
    }
}
