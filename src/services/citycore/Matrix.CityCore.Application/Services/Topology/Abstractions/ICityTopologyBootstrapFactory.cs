using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.Services.Topology.Abstractions
{
    public interface ICityTopologyBootstrapFactory
    {
        CityTopologySeed CreateInitial(City city);
    }
}