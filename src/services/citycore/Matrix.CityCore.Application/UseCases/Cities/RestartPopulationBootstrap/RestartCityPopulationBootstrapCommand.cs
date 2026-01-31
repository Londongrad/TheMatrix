using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed record RestartCityPopulationBootstrapCommand(Guid CityId)
        : IRequest<RestartCityPopulationBootstrapResult>;
}
