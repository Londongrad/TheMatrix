using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.CreateCity
{
    public sealed record CreateCityCommand(
        string Name,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m) : IRequest<Guid>;
}
