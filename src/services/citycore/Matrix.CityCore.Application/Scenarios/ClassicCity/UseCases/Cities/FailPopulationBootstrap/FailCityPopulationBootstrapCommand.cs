using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap
{
    public sealed record FailCityPopulationBootstrapCommand(
        Guid CityId,
        Guid OperationId,
        string FailureCode) : IRequest<bool>;
}
