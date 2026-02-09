import {apiRequest} from "@shared/api/http";
import type {
    CityCreatedView,
    CityListItemView,
    CityView,
    CreateCityRequest,
    RenameCityRequest,
    SimulationKindCatalogItemView,
} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";
import {API_CITY_URL} from "@shared/api/config";

export function getSimulationKinds(signal?: AbortSignal) {
    return apiRequest<SimulationKindCatalogItemView[]>(
        `${API_CITY_URL}/simulation-kinds`,
        {method: "GET", signal},
    );
}

export function getCities(includeArchived: boolean, signal?: AbortSignal) {
    return apiRequest<CityListItemView[]>(
        `${API_CITY_URL}?includeArchived=${includeArchived}`,
        {method: "GET", signal},
    );
}

export function createCity(request: CreateCityRequest) {
    return apiRequest<CityCreatedView>(API_CITY_URL, {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function getCity(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityView>(`${API_CITY_URL}/${cityId}`, {
        method: "GET",
        signal,
    });
}

export function renameCity(cityId: string, request: RenameCityRequest) {
    return apiRequest<void>(`${API_CITY_URL}/${cityId}/name`, {
        method: "PUT",
        body: JSON.stringify(request),
    });
}

export function archiveCity(cityId: string) {
    return apiRequest<void>(`${API_CITY_URL}/${cityId}/archive`, {
        method: "POST",
    });
}

export function deleteCity(cityId: string) {
    return apiRequest<void>(`${API_CITY_URL}/${cityId}`, {
        method: "DELETE",
    });
}
