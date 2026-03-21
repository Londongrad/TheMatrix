using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(ICityBudgetRepository).Assembly); });

            services.AddScoped<CityBudgetAllocationExpenseSupport>();
            services.AddScoped<CityBudgetBusinessDisbursementSupport>();
            services.AddScoped<HouseholdObligationChargeSupport>();
            services.AddScoped<CityBusinessTaxRemittanceSupport>();
            services.AddClassicCityScenarioApplication();
            services.AddScoped<CityMunicipalOperatingCyclePolicy>();
        }
    }
}
