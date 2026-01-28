using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.UseCases.Cities.Common
{
    public sealed record CityDto(
        Guid CityId,
        string Name,
        string Status,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
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
                ClimateZone: city.Environment.ClimateZone.ToString(),
                Hemisphere: city.Environment.Hemisphere.ToString(),
                UtcOffsetMinutes: city.Environment.UtcOffset.TotalMinutes,
                CreatedAtUtc: city.CreatedAtUtc,
                ArchivedAtUtc: city.ArchivedAtUtc,
                IsArchived: city.IsArchived);
        }
    }
}