using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Application.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddScoped<CityEconomyRecurringCycleExecutionService>();
            services.AddScoped<CityBudgetAllocationExpenseSupport>();
            services.AddScoped<CityBudgetBusinessDisbursementSupport>();
            services.AddScoped<CityBudgetOperationalExpenseSupport>();
            services
               .AddScoped<ICityOperationalBudgetPressureProjectionService,
                    CityOperationalBudgetPressureProjectionService>();
            services.AddScoped<HouseholdObligationChargeSupport>();
            services.AddScoped<CityBusinessTaxRemittanceSupport>();
            services.AddScoped<CityMunicipalOperatingCyclePolicy>();

            return services;
        }
    }
}
