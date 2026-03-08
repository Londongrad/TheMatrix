using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityEmploymentWorkplaceSnapshot(
        WorkplaceId WorkplaceId,
        string JobTitle,
        int ResidentCount);
}
