using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RegisterCityBusiness
{
    public sealed record RegisterCityBusinessCommand(
        Guid CityId,
        string Name,
        CityBusinessKind Kind,
        decimal StartingCapital,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol) : IRequest<CityBusinessDto>;
}
