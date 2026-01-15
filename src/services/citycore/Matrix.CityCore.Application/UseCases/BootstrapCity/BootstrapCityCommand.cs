using MediatR;

namespace Matrix.CityCore.Application.UseCases.BootstrapCity
{
    public sealed record BootstrapCityCommand(
        Guid CityId,
        DateTimeOffset StartSimTimeUtc) : IRequest;
}
