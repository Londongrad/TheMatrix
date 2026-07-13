using System.Reflection;
using Matrix.Education.Application.Progression;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Education.Application
{
    public static class DependencyInjection
    {
        public static void AddEducationApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
            services.AddScoped<EducationProgressionBatchProcessorRegistry>();
        }
    }
}
