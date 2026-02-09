using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Topology.Abstractions
{
    public interface ICityTopologyBootstrapFactory
    {
        CityTopologySeed CreateInitial(City city);
    }
}
