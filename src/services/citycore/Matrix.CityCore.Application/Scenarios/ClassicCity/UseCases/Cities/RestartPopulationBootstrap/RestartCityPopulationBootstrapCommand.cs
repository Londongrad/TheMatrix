using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed record RestartCityPopulationBootstrapCommand(Guid CityId)
        : IRequest<RestartCityPopulationBootstrapResult>;
}
