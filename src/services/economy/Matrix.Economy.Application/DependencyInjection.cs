using Matrix.Economy.Application.Abstractions;
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
        }
    }
}
