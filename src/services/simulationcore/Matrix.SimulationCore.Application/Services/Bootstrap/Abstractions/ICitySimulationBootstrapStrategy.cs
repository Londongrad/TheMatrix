using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions
{
    public interface ICitySimulationBootstrapStrategy
    {
        SimulationKind Kind { get; }
        SimulationKindDescriptor Descriptor { get; }

        CitySimulationBootstrapPlan CreatePlan(CreateCityCommand request);
    }
}
