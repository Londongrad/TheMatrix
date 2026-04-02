using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityEmploymentWorkplaceSnapshot(
        WorkplaceId WorkplaceId,
        CityAnchorId? WorkplaceAnchorId,
        string JobTitle,
        int ResidentCount);
}
