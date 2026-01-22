using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.UseCases.Cities.Common
{
    public sealed record CityDto(
        Guid CityId,
        string Name,
        string Status,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ArchivedAtUtc,
        bool IsArchived)
    {
        public static CityDto FromDomain(City city)
        {
            return new CityDto(
                CityId: city.Id.Value,
                Name: city.Name.Value,
                Status: city.Status.ToString(),
                CreatedAtUtc: city.CreatedAtUtc,
                ArchivedAtUtc: city.ArchivedAtUtc,
                IsArchived: city.IsArchived);
        }
    }
}
