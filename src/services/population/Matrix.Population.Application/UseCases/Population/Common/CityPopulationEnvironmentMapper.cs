using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.UseCases.Population.Common
{
    internal static class CityPopulationEnvironmentMapper
    {
        public static CityPopulationEnvironment Create(
            Guid cityId,
            CityPopulationEnvironmentInput input,
            DateTimeOffset createdAtUtc)
        {
            return CityPopulationEnvironment.Create(
                cityId: CityId.From(cityId),
                climateZone: ParseClimateZone(input.ClimateZone),
                hemisphere: ParseHemisphere(input.Hemisphere),
                utcOffsetMinutes: input.UtcOffsetMinutes,
                createdAtUtc: createdAtUtc);
        }

        public static void Sync(
            CityPopulationEnvironment environment,
            CityPopulationEnvironmentInput input,
            DateTimeOffset updatedAtUtc)
        {
            environment.Sync(
                climateZone: ParseClimateZone(input.ClimateZone),
                hemisphere: ParseHemisphere(input.Hemisphere),
                utcOffsetMinutes: input.UtcOffsetMinutes,
                updatedAtUtc: updatedAtUtc);
        }

        internal static PopulationClimateZone ParseClimateZone(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out PopulationClimateZone parsed)
                ? parsed
                : PopulationClimateZone.Unknown;
        }

        internal static PopulationHemisphere ParseHemisphere(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out PopulationHemisphere parsed)
                ? parsed
                : PopulationHemisphere.Unknown;
        }
    }
}
