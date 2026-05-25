using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common
{
    public static class CityTripPurposeNames
    {
        public const string WorkCommute = "WorkCommute";
        public const string EducationCommute = "EducationCommute";
        public const string HealthcareAccess = "HealthcareAccess";
        public const string LeisureWalk = "LeisureWalk";
        public const string ServiceResponse = "ServiceResponse";
        public const string HouseholdRelocation = "HouseholdRelocation";

        public static bool IsSupported(string value)
        {
            string normalized = Normalize(value);

            return normalized == WorkCommute ||
                   normalized == EducationCommute ||
                   normalized == HealthcareAccess ||
                   normalized == LeisureWalk ||
                   normalized == ServiceResponse ||
                   normalized == HouseholdRelocation;
        }

        public static string Normalize(string value)
        {
            return value.Replace(
                        oldValue: "-",
                        newValue: string.Empty,
                        comparisonType: StringComparison.Ordinal)
                   .Replace(
                        oldValue: "_",
                        newValue: string.Empty,
                        comparisonType: StringComparison.Ordinal)
                   .Trim()
                   .ToLowerInvariant() switch
                {
                    "workcommute" => WorkCommute,
                    "educationcommute" => EducationCommute,
                    "healthcareaccess" => HealthcareAccess,
                    "leisurewalk" => LeisureWalk,
                    "serviceresponse" => ServiceResponse,
                    "householdrelocation" => HouseholdRelocation,
                    _ => value.Trim()
                };
        }

        public static CityTripPurpose ToDomain(string value)
        {
            return Normalize(value) switch
            {
                WorkCommute => CityTripPurpose.WorkCommute,
                EducationCommute => CityTripPurpose.EducationCommute,
                HealthcareAccess => CityTripPurpose.HealthcareAccess,
                LeisureWalk => CityTripPurpose.LeisureWalk,
                ServiceResponse => CityTripPurpose.ServiceResponse,
                HouseholdRelocation => CityTripPurpose.HouseholdRelocation,
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    actualValue: value,
                    message: "Unsupported city-trip purpose.")
            };
        }

        public static string FromDomain(CityTripPurpose value)
        {
            return value switch
            {
                CityTripPurpose.WorkCommute => WorkCommute,
                CityTripPurpose.EducationCommute => EducationCommute,
                CityTripPurpose.HealthcareAccess => HealthcareAccess,
                CityTripPurpose.LeisureWalk => LeisureWalk,
                CityTripPurpose.ServiceResponse => ServiceResponse,
                CityTripPurpose.HouseholdRelocation => HouseholdRelocation,
                _ => WorkCommute
            };
        }

        public static string ResolveDefaultSubject(string normalizedPurpose)
        {
            return normalizedPurpose switch
            {
                EducationCommute => "Education commute",
                HealthcareAccess => "Healthcare access",
                LeisureWalk => "Leisure walk",
                ServiceResponse => "Service response",
                HouseholdRelocation => "Household relocation",
                _ => "Work commute"
            };
        }
    }
}
