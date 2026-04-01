using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHealthcareAutonomyPolicy(CityHouseholdLivelihoodPolicy householdLivelihoodPolicy)
    {
        public double ResolveSupportStrength(
            Person resident,
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            DateOnly currentDate,
            CityPopulationServiceQualityState? serviceQualityState = null,
            CityPopulationHealthcarePressureProfile? healthcarePressureProfile = null)
        {
            ArgumentNullException.ThrowIfNull(resident);
            ArgumentNullException.ThrowIfNull(householdResidents);

            if (!resident.IsAlive)
                return 0d;

            CityHouseholdLivelihoodProfile livelihood = householdLivelihoodPolicy.Build(
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                currentDate: currentDate);

            double access = 0.02d +
                            (livelihood.StabilityScore * 0.12d) +
                            (livelihood.IsHoused
                                ? 0.04d
                                : 0d) +
                            (livelihood.AdultProviderCount * 0.03d) +
                            (livelihood.AdultStudentCount * 0.01d) -
                            (livelihood.ActiveIllnessCount > 1
                                ? 0.03d
                                : 0d);

            if (resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior)
                access += 0.03d;

            if (resident.CurrentIllnessSeverity == IllnessSeverity.Severe)
                access += 0.03d;

            if (resident.Employment.Status == EmploymentStatus.Employed)
                access += 0.02d;

            if (!livelihood.HasStructuredSupport)
                access *= 0.60d;

            decimal healthcareQualityIndex = serviceQualityState?.HealthcareQualityIndex ?? 1m;
            access += (double)((healthcareQualityIndex - 1m) * 0.14m);

            if (healthcareQualityIndex < 0.85m)
                access *= 0.92d;

            if (healthcarePressureProfile is not null)
            {
                double recoverySupportMultiplier = Math.Clamp(
                    value: (double)healthcarePressureProfile.RecoverySupportIndex,
                    min: 0.45d,
                    max: 1.35d);
                double triagePressure = Math.Clamp(
                    value: (double)(healthcarePressureProfile.TriagePressureIndex / 3m),
                    min: 0d,
                    max: 1d);

                access *= recoverySupportMultiplier;

                if (resident.CurrentIllnessSeverity == IllnessSeverity.Severe)
                    access += triagePressure * 0.05d;
                else
                    if (resident.CurrentIllnessSeverity == IllnessSeverity.Moderate)
                        access -= triagePressure * 0.01d;
                    else
                        access -= triagePressure * 0.04d;
            }

            return Math.Clamp(
                value: access,
                min: 0d,
                max: 0.48d);
        }
    }
}
