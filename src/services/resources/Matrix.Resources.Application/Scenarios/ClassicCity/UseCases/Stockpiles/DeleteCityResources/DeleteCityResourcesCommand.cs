using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources
{
    public sealed record DeleteCityResourcesCommand(
        Guid CityId,
        DateTimeOffset DeletedAtUtc) : IRequest<DeleteCityResourcesResult>;
}
