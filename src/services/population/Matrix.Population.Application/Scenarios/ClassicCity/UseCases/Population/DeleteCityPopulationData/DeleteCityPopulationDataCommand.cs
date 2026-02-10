using MediatR;

namespace Matrix.Population.Application.UseCases.Population.DeleteCityPopulationData
{
    public sealed record DeleteCityPopulationDataCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset DeletedAtUtc) : IRequest<DeleteCityPopulationDataResult>;
}
