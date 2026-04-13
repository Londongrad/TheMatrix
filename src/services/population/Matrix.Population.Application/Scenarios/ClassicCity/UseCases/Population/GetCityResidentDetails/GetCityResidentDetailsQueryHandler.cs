using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails
{
    public sealed class GetCityResidentDetailsQueryHandler(
        ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
        ICityPopulationCommuteRoutingService cityPopulationCommuteRoutingService,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
        IPersonReadRepository personReadRepository)
        : IRequestHandler<GetCityResidentDetailsQuery, CityResidentDetailsDto>
    {
        public async Task<CityResidentDetailsDto> Handle(
            GetCityResidentDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = CityId.From(request.CityId);
            Person resident = await cityPopulationPersonReadRepository.FindByCityAndPersonIdAsync(
                                  cityId: cityId,
                                  personId: PersonId.From(request.PersonId),
                                  cancellationToken: cancellationToken) ??
                              throw ApplicationErrorsFactory.PersonNotFound(request.PersonId);

            Person? currentSpouse = resident.SpouseId is not
                { } spouseId
                ? null
                : await personReadRepository.FindByIdAsync(
                    id: spouseId,
                    cancellationToken: cancellationToken);
            Person? mother = resident.MotherId is not
                { } motherId
                ? null
                : await personReadRepository.FindByIdAsync(
                    id: motherId,
                    cancellationToken: cancellationToken);
            Person? father = resident.FatherId is not
                { } fatherId
                ? null
                : await personReadRepository.FindByIdAsync(
                    id: fatherId,
                    cancellationToken: cancellationToken);
            IReadOnlyCollection<Person> children = await cityPopulationPersonReadRepository.ListChildrenByParentIdAsync(
                cityId: cityId,
                parentId: resident.Id,
                cancellationToken: cancellationToken);
            CityResidentHousingSnapshot? housing =
                await cityPopulationPersonReadRepository.FindHousingSnapshotByPersonIdAsync(
                    cityId: cityId,
                    personId: resident.Id,
                    cancellationToken: cancellationToken);
            IReadOnlyList<CityPopulationAnchorCatalogItem> hospitalAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: cityId,
                    type: CityAnchorType.Hospital,
                    cancellationToken: cancellationToken);
            CityPopulationAnchorCatalogItem? primaryCareAnchor = anchorSelectionPolicy.SelectHospitalAnchor(
                anchors: hospitalAnchors,
                preferredDistrictId: housing?.DistrictId,
                stableKey: resident.Id.Value);
            ResidentialBuildingId? residentialBuildingId = housing?.ResidentialBuildingId;
            CityPopulationCommuteContext? workplaceRouteAccess = resident.Employment.Job is null
                ? null
                : await cityPopulationCommuteRoutingService.ResolveEmploymentCommuteAsync(
                    cityId: cityId.Value,
                    residentialBuildingId: residentialBuildingId,
                    resident: resident,
                    cancellationToken: cancellationToken);
            CityPopulationCommuteContext? educationRouteAccess = resident.Education.CurrentInstitutionId is null
                ? null
                : await cityPopulationCommuteRoutingService.ResolveEducationCommuteAsync(
                    cityId: cityId.Value,
                    residentialBuildingId: residentialBuildingId,
                    resident: resident,
                    cancellationToken: cancellationToken);
            CityPopulationCommuteContext? healthcareRouteAccess = primaryCareAnchor is null
                ? null
                : await cityPopulationCommuteRoutingService.ResolveHealthcareCommuteAsync(
                    cityId: cityId.Value,
                    residentialBuildingId: residentialBuildingId,
                    healthcareAnchorId: primaryCareAnchor.CityAnchorId,
                    cancellationToken: cancellationToken);
            CityResidentHealthcareProviderDto? primaryHealthcareProvider = primaryCareAnchor is null
                ? null
                : new CityResidentHealthcareProviderDto(
                    PrimaryCareAnchorId: primaryCareAnchor.CityAnchorId.Value,
                    RouteAccess: new CityResidentRouteAccessDto(
                        HasRouteData: healthcareRouteAccess?.HasRouteData ?? false,
                        IsAccessible: healthcareRouteAccess?.IsAccessible ?? true,
                        AccessibilityIndex: healthcareRouteAccess?.AccessibilityIndex ?? 1m,
                        PassabilityIndex: healthcareRouteAccess?.PassabilityIndex ?? 1m,
                        EstimatedTravelTimeMinutes: healthcareRouteAccess?.EstimatedTravelTimeMinutes));

            return resident.ToResidentDetailsDto(
                currentDate: request.CurrentDate,
                currentSpouse: currentSpouse,
                currentHousing: housing,
                mother: mother,
                father: father,
                children: children,
                workplaceRouteAccess: workplaceRouteAccess,
                educationRouteAccess: educationRouteAccess,
                primaryHealthcareProvider: primaryHealthcareProvider);
        }
    }
}
