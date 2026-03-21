using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap
{
    public sealed record FailCityEconomyBootstrapCommand(
        Guid CityId,
        Guid OperationId,
        string FailureCode) : IRequest<bool>;
}
