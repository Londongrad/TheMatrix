using MediatR;

namespace Matrix.Population.Application.UseCases.Population.AdvanceCityPopulation
{
    public sealed record AdvanceCityPopulationCommand(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId) : IRequest<AdvanceCityPopulationResult>;
}
