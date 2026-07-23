using System.Reflection;
using Matrix.Education.Domain.Scenarios.ClassicCity.Attendance;
using Matrix.Education.Application.Progression;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Integration;
using Matrix.Education.Application.Scenarios.ClassicCity.Participation;
using Matrix.Education.Application.Scenarios.ClassicCity.Progression;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Education.Application
{
    public static class DependencyInjection
    {
        public static void AddEducationApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
            services.AddSingleton<ClassicCityEducationProgressionPolicy>();
            services.AddSingleton<ClassicCityLearningAttendancePolicy>();
            services.AddSingleton<ClassicCityEducationInstitutionSelectionPolicy>();
            services.AddScoped<IEducationProgressionBatchProcessor,
                ClassicCityEducationProgressionBatchProcessor>();
            services.AddScoped<EducationProgressionBatchProcessorRegistry>();
            services.AddSingleton<IEducationParticipationEconomicPolicy, ClassicCityEducationEconomicPolicy>();
            services.AddSingleton<EducationEconomicPolicyRegistry>();
            services.AddSingleton<IEducationParticipationRoutinePolicy, ClassicCityEducationRoutinePolicy>();
            services.AddSingleton<EducationRoutinePolicyRegistry>();
            services.AddScoped<IEducationStudentParticipationOutboxWriter, EducationStudentParticipationPublisher>();
        }
    }
}
