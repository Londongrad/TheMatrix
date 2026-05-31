using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Bootstrap.Abstractions
{
    public interface IClassicCityBootstrapFactory
    {
        bool SupportsAutomaticPopulationBootstrap { get; }

        ClassicCityBootstrapPlan CreatePlan(CreateCityCommand request);
    }
}
