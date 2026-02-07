using Matrix.CityCore.Application.UseCases.Cities.CreateCity;
using Matrix.CityCore.Domain.Cities.Enums;

namespace Matrix.CityCore.Application.Services.Bootstrap.Abstractions
{
    public interface ICitySimulationBootstrapStrategy
    {
        SimulationKind Kind { get; }

        CitySimulationBootstrapPlan CreatePlan(CreateCityCommand request);
    }
}
