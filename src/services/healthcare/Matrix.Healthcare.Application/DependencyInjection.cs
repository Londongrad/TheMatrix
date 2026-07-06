using System.Reflection;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Care.AllocatePatientCare;
using Matrix.Healthcare.Application.Care.DeliverPatientCare;
using Matrix.Healthcare.Application.Operations;
using Matrix.Healthcare.Domain.Care;
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
            services.AddSingleton<PatientFunctionalCapacityPolicy>();
            services.AddSingleton<PatientCareNeedAssessmentPolicy>();
            services.AddSingleton<PatientCareAllocationPolicy>();
            services.AddSingleton<PatientCareTreatmentPolicy>();
            services.AddSingleton<PatientCareDeliveryService>();
            services.AddScoped<IPatientCareAllocator, PatientCareAllocator>();
            services.AddScoped<ICareOperationalProfileProvider, CareOperationalProfileProvider>();
        }
    }
}
