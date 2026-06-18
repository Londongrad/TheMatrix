using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.Common
{
    internal static class CityEmploymentOperationSupport
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

        public static async Task<Job> CreateJobAsync(
            Guid cityId,
            Person resident,
            string? jobTitle,
            CityEmploymentWorkplaceSnapshot? workplace,
            CityResidentHousingSnapshot? housing,
            ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            CancellationToken cancellationToken)
        {
            if (workplace is not null)
                return new Job(
                    workplaceId: workplace.WorkplaceId,
                    title: workplace.JobTitle,
                    workplaceAnchorId: workplace.WorkplaceAnchorId);

            string normalizedTitle = GuardHelper.AgainstNullOrWhiteSpace(
                value: jobTitle,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: "JobTitle");
            IReadOnlyList<CityPopulationAnchorCatalogItem> workplaceAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: CityId.From(cityId),
                    type: CityAnchorType.Workplace,
                    cancellationToken: cancellationToken);
            CityAnchorId? workplaceAnchorId = anchorSelectionPolicy.SelectWorkplaceAnchor(
                    anchors: workplaceAnchors,
                    preferredDistrictId: housing?.DistrictId,
                    stableKey: resident.Id.Value)
              ?.CityAnchorId;

            return new Job(
                workplaceId: WorkplaceId.New(),
                title: normalizedTitle.Trim(),
                workplaceAnchorId: workplaceAnchorId);
        }

        public static void EnsureResidentCanBeFired(Person resident)
        {
            if (!resident.IsAlive)
                throw DomainErrorsFactory.DeceasedPersonCannotBeFired(nameof(resident));

            if (resident.Employment.Status != EmploymentStatus.Employed)
                throw DomainErrorsFactory.UnemployedPersonCannotBeFired(nameof(resident));
        }

        public static void EnsureResidentCanRetire(
            Person resident,
            DateOnly currentDate)
        {
            if (!resident.IsAlive)
                throw DomainErrorsFactory.DeceasedPersonCannotRetire(nameof(resident));

            if (resident.GetAgeGroup(currentDate) != AgeGroup.Senior)
                throw DomainErrorsFactory.OnlySeniorsCanRetire(nameof(resident));
        }

        public static CityEmploymentOperationResultDto CreateResult(
            string action,
            DateTimeOffset recordedAtUtc,
            DateOnly currentDate,
            Person resident)
        {
            return new CityEmploymentOperationResultDto(
                Action: action,
                RecordedAtUtc: recordedAtUtc,
                Resident: resident.ToResidentDetailsDto(currentDate));
        }
    }
}
