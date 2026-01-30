using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.CompletePopulationBootstrap
{
    public sealed record CompleteCityPopulationBootstrapCommand(Guid CityId) : IRequest<bool>;
}
