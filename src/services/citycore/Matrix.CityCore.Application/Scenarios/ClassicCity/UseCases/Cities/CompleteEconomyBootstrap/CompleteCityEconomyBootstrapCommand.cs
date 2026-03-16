using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap
{
    public sealed record CompleteCityEconomyBootstrapCommand(
        Guid CityId,
        Guid OperationId) : IRequest<bool>;
}
