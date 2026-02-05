using MediatR;

namespace Matrix.Population.Application.UseCases.Population.ArchiveCityPopulationData
{
    public sealed record ArchiveCityPopulationDataCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset ArchivedAtUtc) : IRequest<ArchiveCityPopulationDataResult>;
}
