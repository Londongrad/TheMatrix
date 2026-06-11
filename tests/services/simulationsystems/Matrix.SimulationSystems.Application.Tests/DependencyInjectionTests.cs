using Matrix.SimulationSystems.Application.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests
{
    public sealed class DependencyInjectionTests
    {
        [Fact]
        public void AddApplication_DoesNotRegisterClassicCityServices()
        {
            var services = new ServiceCollection();

            services.AddApplication();

            Assert.DoesNotContain(
                collection: services,
                filter: descriptor => descriptor.ServiceType == typeof(CityEnvironmentalConditionPolicy));
        }

        [Fact]
        public void AddClassicCityScenarioApplication_RegistersScenarioServices()
        {
            var services = new ServiceCollection();

            services.AddClassicCityScenarioApplication();

            Assert.Contains(
                collection: services,
                filter: descriptor => descriptor.ServiceType == typeof(CityEnvironmentalConditionPolicy));
        }
    }
}
