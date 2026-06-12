using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.DependencyInjection;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Errors;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            services.AddValidatorsFromAssembly(assembly);
            services.AddScoped<IValidationExceptionFactory, EconomyValidationErrorFactory>();

            services.AddScoped<CityBudgetAllocationExpenseSupport>();
            services.AddScoped<CityBudgetBusinessDisbursementSupport>();
            services.AddScoped<CityBudgetOperationalExpenseSupport>();
            services
               .AddScoped<ICityOperationalBudgetPressureProjectionService,
                    CityOperationalBudgetPressureProjectionService>();
            services.AddScoped<HouseholdObligationChargeSupport>();
            services.AddScoped<CityBusinessTaxRemittanceSupport>();
            services.AddScoped<CityMunicipalOperatingCyclePolicy>();

            services.AddDefaultApplicationPipeline();
        }
    }
}
