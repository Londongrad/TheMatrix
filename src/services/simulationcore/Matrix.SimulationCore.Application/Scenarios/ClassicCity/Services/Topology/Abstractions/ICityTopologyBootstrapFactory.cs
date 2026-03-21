using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology.Abstractions
{
    public interface ICityTopologyBootstrapFactory
    {
        CityTopologySeed CreateInitial(City city);
    }
}
