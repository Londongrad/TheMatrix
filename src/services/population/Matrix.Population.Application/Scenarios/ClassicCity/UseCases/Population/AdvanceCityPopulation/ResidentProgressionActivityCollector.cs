using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using PersonId = Matrix.Population.Domain.ValueObjects.PersonId;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentProgressionActivityCollector
    {
        internal static Snapshot Capture(PersonEntity person)
        {
            return new Snapshot(
                IsAlive: person.IsAlive,
                MaritalStatus: person.MaritalStatus,
                SpouseId: person.SpouseId,
                EmploymentStatus: person.Employment.Status,
                JobTitle: person.Employment.Job?.Title,
                EducationLevel: person.EducationLevel);
        }

        internal static void Collect(
            CityId cityId,
            DateOnly currentDate,
            Snapshot before,
            PersonEntity resident,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            DateTimeOffset occurredAtUtc)
        {
            if (before.IsAlive && !resident.IsAlive)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentDied(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));

            if (before.MaritalStatus != MaritalStatus.Widowed && resident.MaritalStatus == MaritalStatus.Widowed)
            {
                string deceasedName = before.SpouseId is not null &&
                                      residentsById.TryGetValue(
                                          key: before.SpouseId.Value,
                                          value: out PersonEntity? spouse)
                    ? spouse.Name.ToString()
                    : "their spouse";

                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentBecameWidowed(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        deceasedName: deceasedName,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
            }

            if (before.EducationLevel != resident.EducationLevel && resident.EducationLevel > before.EducationLevel)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentGraduated(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));

            if (before.EmploymentStatus != EmploymentStatus.Student &&
                resident.Employment.Status == EmploymentStatus.Student)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentEnrolled(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
            else
                if (before.EmploymentStatus == EmploymentStatus.Student &&
                    resident.Employment.Status != EmploymentStatus.Student)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentWithdrewFromStudy(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));

            if (before.EmploymentStatus != EmploymentStatus.Employed &&
                resident.Employment.Status == EmploymentStatus.Employed)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentHired(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
            else
                if (before.EmploymentStatus == EmploymentStatus.Employed &&
                    resident.Employment.Status == EmploymentStatus.Unemployed)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentFired(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        previousJobTitle: before.JobTitle,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
            else
                    if (before.EmploymentStatus != EmploymentStatus.Retired &&
                        resident.Employment.Status == EmploymentStatus.Retired)
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentRetired(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
        }

        internal sealed record Snapshot(
            bool IsAlive,
            MaritalStatus MaritalStatus,
            PersonId? SpouseId,
            EmploymentStatus EmploymentStatus,
            string? JobTitle,
            EducationLevel EducationLevel);
    }
}
