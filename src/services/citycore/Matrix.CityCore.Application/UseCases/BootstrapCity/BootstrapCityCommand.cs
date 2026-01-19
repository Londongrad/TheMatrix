using MediatR;

namespace Matrix.CityCore.Application.UseCases.BootstrapCity
{
    public sealed record BootstrapCityCommand(
        DateTimeOffset StartSimTimeUtc) : IRequest<Guid>;
}
