using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData
{
    public sealed record DeleteCitySystemsDataCommand(
        Guid CityId,
        DateTimeOffset DeletedAtUtc) : IRequest<DeleteCitySystemsDataResult>;
}
