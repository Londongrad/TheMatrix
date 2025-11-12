using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Matrix.Population.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<PopulationGenerator>();

            services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(IPersonReadRepository).Assembly);
            });
        }
    }
}
