import {API_CITY_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {PagedResult} from "@shared/lib/paging/pagingTypes";
import type {
    CityCivilRegistryOperationResultDto,
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
