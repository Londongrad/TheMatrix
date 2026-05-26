using MediatR;

namespace Matrix.Economy.Application.UseCases.Lifecycle.DeleteCityEconomyData
{
    public sealed record DeleteCityEconomyDataCommand(
        Guid CityId,
        DateTimeOffset DeletedAtUtc) : IRequest<DeleteCityEconomyDataResult>;
}
