using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Generation;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Application.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddSingleton<IPopulationGenerationContentCatalog, PopulationGenerationContentCatalog>();
            services.AddSingleton<CityPopulationBootstrapGenerator>();
            services.AddSingleton<CityHouseholdLivelihoodPolicy>();
            services.AddSingleton<CityHouseholdCashflowPolicy>();
            services.AddSingleton<CityHouseholdEconomyPolicy>();
            services.AddSingleton<CityCivilRegistryAutonomyPolicy>();
            services.AddSingleton<CityBirthAutonomyPolicy>();
            services.AddSingleton<CityEducationAutonomyPolicy>();
            services.AddSingleton<CityEmploymentAutonomyPolicy>();
            services.AddSingleton<CityHealthcareAutonomyPolicy>();
            services.AddSingleton<CityHouseholdPressurePolicy>();
            services.AddSingleton<CityHousingAutonomyPolicy>();
            services.AddSingleton<CityHouseholdIndependenceAutonomyPolicy>();
            services.AddSingleton<CityPopulationAnchorSelectionPolicy>();
            services.AddSingleton<CityPopulationDistrictImpactPolicy>();
            services.AddSingleton<CityPopulationHealthcarePressurePolicy>();
            services.AddSingleton<CityIllnessAutonomyPolicy>();
            services.AddSingleton<CityPopulationClimateAdaptationPolicy>();
            services.AddSingleton<CityPopulationLivingConditionsPressurePolicy>();
            services.AddSingleton<CityPopulationParticipationPolicy>();
            services.AddSingleton<CityPopulationWeatherImpactPolicy>();
            services.AddSingleton<CityPopulationWeatherExposurePolicy>();
            services.AddScoped<ICityPopulationCommuteRoutingService, CityPopulationCommuteRoutingService>();
            services.AddScoped<ICityPopulationCommuteTripSyncService, CityPopulationCommuteTripSyncService>();
            services.AddScoped<IPersonLifecycleExtension, ClassicCityPersonLifecycleExtension>();

            return services;
        }
    }
}
