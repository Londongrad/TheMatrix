using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.FailPopulationBootstrap
{
    public sealed record FailCityPopulationBootstrapCommand(
        Guid CityId,
        string Error) : IRequest<bool>;
}
