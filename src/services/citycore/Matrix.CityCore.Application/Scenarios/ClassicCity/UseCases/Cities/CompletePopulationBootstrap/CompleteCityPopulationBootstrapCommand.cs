using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap
{
    public sealed record CompleteCityPopulationBootstrapCommand(
        Guid CityId,
        Guid OperationId) : IRequest<bool>;
}
