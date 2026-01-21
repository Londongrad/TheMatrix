using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.BootstrapCity
{
    public sealed record BootstrapCityCommand(DateTimeOffset StartSimTimeUtc) : IRequest<Guid>;
}
