using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;

namespace Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions
{
    public interface ICitySimulationBootstrapStrategy
    {
        SimulationKindDescriptor Descriptor { get; }

        CitySimulationBootstrapPlan CreatePlan(CreateCityCommand request);
    }
}
