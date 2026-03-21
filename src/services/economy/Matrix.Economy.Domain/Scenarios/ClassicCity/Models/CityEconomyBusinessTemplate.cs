using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomyBusinessTemplate(
        string TemplateKey,
        string Name,
        CityBusinessKind Kind,
        Money StartingCapital);
}
