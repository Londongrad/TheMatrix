using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.Common
{
    internal static class CityEducationOperationSupport
    {
        public static async Task<Person> LoadResidentInCityAsync(
            Guid cityId,
            Guid residentId,
            IPersonReadRepository personReadRepository,
            ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
            CancellationToken cancellationToken)
        {
            Person resident = await personReadRepository.FindByIdAsync(
                                  id: PersonId.From(residentId),
                                  cancellationToken: cancellationToken) ??
                              throw ApplicationErrorsFactory.PersonNotFound(residentId);

            CityId? actualCityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: resident.Id,
                cancellationToken: cancellationToken);

            if (actualCityId is null || actualCityId.Value.Value != cityId)
                throw ApplicationErrorsFactory.PersonNotAssignedToCity(
                    personId: residentId,
                    cityId: cityId);

            return resident;
        }

        public static void EnsureResidentCanEnroll(
            Person resident,
            DateOnly currentDate)
        {
            if (!resident.IsAlive)
                throw ApplicationErrorsFactory.DeceasedResidentCannotStudy(
                    residentId: resident.Id.Value,
                    action: "start studying");

            if (resident.Employment.Status == EmploymentStatus.Student)
                throw ApplicationErrorsFactory.ResidentAlreadyStudent(resident.Id.Value);

            if (resident.Employment.Status == EmploymentStatus.Retired)
                throw ApplicationErrorsFactory.RetiredResidentCannotStudy(resident.Id.Value);

            if (resident.GetAgeGroup(currentDate) == AgeGroup.Senior)
                throw ApplicationErrorsFactory.SeniorResidentCannotStudy(resident.Id.Value);
        }

        public static void EnsureResidentCanWithdraw(Person resident)
        {
            if (!resident.IsAlive)
                throw ApplicationErrorsFactory.DeceasedResidentCannotStudy(
                    residentId: resident.Id.Value,
                    action: "withdraw from study");

            if (resident.Employment.Status != EmploymentStatus.Student)
                throw ApplicationErrorsFactory.ResidentMustBeStudent(
                    residentId: resident.Id.Value,
                    action: "withdraw from study");
        }

        public static EducationLevel ParseTargetEducationLevel(string? value)
        {
            string normalizedValue = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: "TargetEducationLevel");

            if (!Enum.TryParse(
                    value: normalizedValue.Trim(),
                    ignoreCase: true,
                    result: out EducationLevel parsedLevel))
                throw ApplicationErrorsFactory.InvalidEducationLevel(normalizedValue);

            return parsedLevel;
        }

        public static void EnsureResidentCanGraduate(
            Person resident,
            EducationLevel targetEducationLevel)
        {
            if (!resident.IsAlive)
                throw ApplicationErrorsFactory.DeceasedResidentCannotStudy(
                    residentId: resident.Id.Value,
                    action: "graduate");

            if (resident.Employment.Status != EmploymentStatus.Student)
                throw ApplicationErrorsFactory.ResidentMustBeStudent(
                    residentId: resident.Id.Value,
                    action: "graduate");

            if (resident.EducationLevel == targetEducationLevel)
                throw ApplicationErrorsFactory.ResidentAlreadyAtEducationLevel(
                    residentId: resident.Id.Value,
                    educationLevel: targetEducationLevel.ToString());
        }

        public static async Task<CityEducationInstitutionSnapshot?> ResolveInstitutionAsync(
            Guid cityId,
            Guid? institutionId,
            EducationLevel expectedEducationLevel,
            ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
            CancellationToken cancellationToken)
        {
            if (!institutionId.HasValue)
                return null;

            CityEducationInstitutionSnapshot? institution =
                await cityPopulationPersonReadRepository.FindEducationInstitutionByIdAsync(
                    cityId: CityId.From(cityId),
                    institutionId: EducationInstitutionId.From(institutionId.Value),
                    cancellationToken: cancellationToken);

            if (institution is null)
                throw ApplicationErrorsFactory.EducationInstitutionNotFound(
                    institutionId: institutionId.Value,
                    cityId: cityId);

            if (institution.EducationLevel != expectedEducationLevel)
                throw ApplicationErrorsFactory.EducationInstitutionLevelMismatch(
                    institutionId: institutionId.Value,
                    expectedEducationLevel: expectedEducationLevel.ToString(),
                    actualEducationLevel: institution.EducationLevel.ToString());

            return institution;
        }

        public static async Task<CityEducationInstitutionBinding> CreateInstitutionBindingAsync(
            Guid cityId,
            Person resident,
            CityEducationInstitutionSnapshot? institution,
            CityResidentHousingSnapshot? housing,
            EducationLevel educationLevel,
            ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            CancellationToken cancellationToken)
        {
            if (institution is not null)
                return new CityEducationInstitutionBinding(
                    InstitutionId: institution.InstitutionId,
                    InstitutionAnchorId: institution.InstitutionAnchorId);

            IReadOnlyList<CityPopulationAnchorCatalogItem> schoolAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: CityId.From(cityId),
                    type: CityAnchorType.School,
                    cancellationToken: cancellationToken);
            CityAnchorId? institutionAnchorId = anchorSelectionPolicy.SelectSchoolAnchor(
                    anchors: schoolAnchors,
                    preferredDistrictId: housing?.DistrictId,
                    stableKey: resident.Id.Value)
              ?.CityAnchorId;

            return new CityEducationInstitutionBinding(
                InstitutionId: EducationInstitutionId.New(),
                InstitutionAnchorId: institutionAnchorId);
        }

        public static CityEducationOperationResultDto CreateResult(
            string action,
            DateTimeOffset recordedAtUtc,
            DateOnly currentDate,
            Person resident)
        {
            return new CityEducationOperationResultDto(
                Action: action,
                RecordedAtUtc: recordedAtUtc,
                Resident: resident.ToResidentDetailsDto(currentDate));
        }
    }
}
