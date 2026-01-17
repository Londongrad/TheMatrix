using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.CityCore.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            return services;
        }
    }
}
