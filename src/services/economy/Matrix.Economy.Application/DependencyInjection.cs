using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(ICityBudgetRepository).Assembly);
            });

            services.AddScoped<HouseholdObligationChargeSupport>();
            services.AddScoped<CityBusinessTaxRemittanceSupport>();
        }
    }
}
