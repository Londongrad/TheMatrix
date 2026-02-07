using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.UseCases.Cities.Common
{
    public sealed record CityDto(
        Guid CityId,
        string Name,
        string SimulationKind,
        string Status,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string GenerationSeed,
        string SizeTier,
        string UrbanDensity,
        string DevelopmentLevel,
        Guid PopulationBootstrapOperationId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? PopulationBootstrapCompletedAtUtc,
        DateTimeOffset? PopulationBootstrapFailedAtUtc,
        string? PopulationBootstrapFailureCode,
        DateTimeOffset? ArchivedAtUtc,
        bool IsArchived)
    {
        public static CityDto FromDomain(City city)
        {
            return new CityDto(
                CityId: city.Id.Value,
                Name: city.Name.Value,
                SimulationKind: city.SimulationKind.ToString(),
                Status: city.Status.ToString(),
                ClimateZone: city.Environment.ClimateZone.ToString(),
                Hemisphere: city.Environment.Hemisphere.ToString(),
                UtcOffsetMinutes: city.Environment.UtcOffset.TotalMinutes,
                GenerationSeed: city.GenerationSeed.Value,
                SizeTier: city.GenerationProfile.SizeTier.ToString(),
                UrbanDensity: city.GenerationProfile.UrbanDensity.ToString(),
                DevelopmentLevel: city.GenerationProfile.DevelopmentLevel.ToString(),
                PopulationBootstrapOperationId: city.PopulationBootstrapOperationId,
                CreatedAtUtc: city.CreatedAtUtc,
                PopulationBootstrapCompletedAtUtc: city.PopulationBootstrapCompletedAtUtc,
                PopulationBootstrapFailedAtUtc: city.PopulationBootstrapFailedAtUtc,
                PopulationBootstrapFailureCode: city.PopulationBootstrapFailureCode,
                ArchivedAtUtc: city.ArchivedAtUtc,
                IsArchived: city.IsArchived);
        }
    }
}
