using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData
{
    public sealed record DeleteCityPopulationDataCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset DeletedAtUtc) : IRequest<DeleteCityPopulationDataResult>;
}
