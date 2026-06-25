using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Healthcare.Application
{
    public static class DependencyInjection
    {
        public static void AddHealthcareApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        }
    }
}
