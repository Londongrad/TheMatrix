import {API_CITY_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {PagedResult} from "@shared/lib/paging/pagingTypes";
import type {PersonDto} from "@services/population/person/api/personTypes";

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
