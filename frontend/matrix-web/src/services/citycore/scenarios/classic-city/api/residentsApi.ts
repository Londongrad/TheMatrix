import {API_CITY_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {PagedResult} from "@shared/lib/paging/pagingTypes";
import type {
    CityCivilRegistryOperationResultDto,
    CityEducationOperationResultDto,
    CityEmploymentCatalogDto,
    CityEmploymentOperationResultDto,
    CityResidentDetailsDto,
    PersonDto,
} from "@services/population/person/api/personTypes";

export function getCityResidentsPage(
    cityId: string,
    pageNumber: number,
    pageSize: number,
) {
    return apiRequest<PagedResult<PersonDto>>(
        `${API_CITY_URL}/${cityId}/residents?pageNumber=${pageNumber}&pageSize=${pageSize}`,
        {
            method: "GET",
        },
    );
}

export function getCityResidentDetails(
    cityId: string,
    residentId: string,
    signal?: AbortSignal,
) {
    return apiRequest<CityResidentDetailsDto>(
        `${API_CITY_URL}/${cityId}/residents/${residentId}`,
        {
            method: "GET",
            signal,
        },
    );
}

type CityCivilRegistryOperationPayload = {
    firstResidentId: string;
    secondResidentId: string;
};

type CityEmploymentOperationPayload = {
    residentId: string;
    jobTitle?: string | null;
    workplaceId?: string | null;
};

type CityEducationOperationPayload = {
    residentId: string;
    targetEducationLevel?: string | null;
};

export function getCityEmploymentCatalog(
    cityId: string,
    signal?: AbortSignal,
) {
    return apiRequest<CityEmploymentCatalogDto>(
        `${API_CITY_URL}/${cityId}/employment/catalog`,
        {
            method: "GET",
            signal,
        },
    );
}

export function hireCityResident(
    cityId: string,
    payload: CityEmploymentOperationPayload,
) {
    return apiRequest<CityEmploymentOperationResultDto>(
        `${API_CITY_URL}/${cityId}/employment/hire`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function fireCityResident(
    cityId: string,
    payload: CityEmploymentOperationPayload,
) {
    return apiRequest<CityEmploymentOperationResultDto>(
        `${API_CITY_URL}/${cityId}/employment/fire`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function retireCityResident(
    cityId: string,
    payload: CityEmploymentOperationPayload,
) {
    return apiRequest<CityEmploymentOperationResultDto>(
        `${API_CITY_URL}/${cityId}/employment/retire`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function enrollCityResident(
    cityId: string,
    payload: CityEducationOperationPayload,
) {
    return apiRequest<CityEducationOperationResultDto>(
        `${API_CITY_URL}/${cityId}/education/enroll`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function graduateCityResident(
    cityId: string,
    payload: CityEducationOperationPayload,
) {
    return apiRequest<CityEducationOperationResultDto>(
        `${API_CITY_URL}/${cityId}/education/graduate`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function withdrawCityResidentFromStudy(
    cityId: string,
    payload: CityEducationOperationPayload,
) {
    return apiRequest<CityEducationOperationResultDto>(
        `${API_CITY_URL}/${cityId}/education/withdraw`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function registerCityMarriage(
    cityId: string,
    payload: CityCivilRegistryOperationPayload,
) {
    return apiRequest<CityCivilRegistryOperationResultDto>(
        `${API_CITY_URL}/${cityId}/civil-registry/marriages`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function registerCityDivorce(
    cityId: string,
    payload: CityCivilRegistryOperationPayload,
) {
    return apiRequest<CityCivilRegistryOperationResultDto>(
        `${API_CITY_URL}/${cityId}/civil-registry/divorces`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}
