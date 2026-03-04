import {API_CITY_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {PagedResult} from "@shared/lib/paging/pagingTypes";
import type {CityResidentDetailsDto, PersonDto} from "@services/population/person/api/personTypes";

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
