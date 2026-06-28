using System.Reflection;
using Matrix.Healthcare.Domain.Progression;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Healthcare.Application
{
    public static class DependencyInjection
    {
        public static void AddHealthcareApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
            services.AddSingleton<PatientMedicalRiskRoll>();
            services.AddSingleton<PatientIllnessDiagnosisPolicy>();
            services.AddSingleton<PatientIllnessCoursePolicy>();
            services.AddSingleton<PatientIllnessBurdenPolicy>();
            services.AddSingleton<PatientIllnessProgressionPolicy>();
        }
    }
}
