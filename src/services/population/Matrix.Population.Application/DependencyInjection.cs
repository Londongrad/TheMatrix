using Matrix.Population.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(IPersonReadRepository).Assembly);
            });
        }
    }
}
