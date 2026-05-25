import {apiRequest} from "@shared/api/http";
import type {
    CityDashboardView,
    CityListItemView,
    CityProvisioningStatusView,
    CityProvisioningView,
    CityView,
    CreateCityRequest,
    RenameCityRequest,
    RetryPopulationBootstrapRequest,
} from "@services/simulationcore/scenarios/classic-city/contracts/citiesContracts";
import type {
    CityActiveTripView,
    CityMapTopologyView,
} from "@services/simulationcore/scenarios/classic-city/contracts/worldContracts";
import type {
    CityDistrictInfrastructureView
} from "@services/simulationcore/scenarios/classic-city/contracts/infrastructureContracts";
import type {
    CityUtilityIncidentStatusView,
    DispatchCityResupplyRequest,
    DispatchCityResupplyView,
    DispatchCityUtilityIncidentResponseRequest,
} from "@services/simulationcore/scenarios/classic-city/contracts/operatorContracts";
import {API_CITY_URL} from "@shared/api/config";

export function getCities(includeArchived: boolean, signal?: AbortSignal) {
    return apiRequest<CityListItemView[]>(
        `${API_CITY_URL}?includeArchived=${includeArchived}`,
        {method: "GET", signal},
    );
}

export function getProvisioningCities(signal?: AbortSignal) {
    return apiRequest<CityListItemView[]>(`${API_CITY_URL}/provisioning`, {
        method: "GET",
        signal,
    });
}

export function createCity(request: CreateCityRequest) {
    return apiRequest<CityProvisioningView>(API_CITY_URL, {
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

export function getCityDashboard(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityDashboardView>(`${API_CITY_URL}/${cityId}/dashboard`, {
        method: "GET",
        signal,
    });
}

export function getCityMapTopology(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityMapTopologyView>(`${API_CITY_URL}/${cityId}/map`, {
        method: "GET",
        signal,
    });
}

export function getCityActiveTrips(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityActiveTripView[]>(`${API_CITY_URL}/${cityId}/trips/active`, {
        method: "GET",
        signal,
    });
}

export function getCityDistrictInfrastructure(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityDistrictInfrastructureView>(`${API_CITY_URL}/${cityId}/infrastructure/districts`, {
        method: "GET",
        signal,
    });
}

export function dispatchDistrictUtilityResponse(
    cityId: string,
    request: DispatchCityUtilityIncidentResponseRequest,
) {
    return apiRequest<CityUtilityIncidentStatusView>(`${API_CITY_URL}/${cityId}/operator/utility-response`, {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function dispatchDistrictResupply(
    cityId: string,
    request: DispatchCityResupplyRequest,
) {
    return apiRequest<DispatchCityResupplyView>(`${API_CITY_URL}/${cityId}/operator/resupply`, {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function renameCity(cityId: string, request: RenameCityRequest) {
    return apiRequest<void>(`${API_CITY_URL}/${cityId}/name`, {
        method: "PUT",
        body: JSON.stringify(request),
    });
}

export function getCityProvisioning(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityProvisioningStatusView>(`${API_CITY_URL}/${cityId}/provisioning`, {
        method: "GET",
        signal,
    });
}

export function retryPopulationBootstrap(
    cityId: string,
    request?: RetryPopulationBootstrapRequest,
) {
    return apiRequest<CityProvisioningView>(`${API_CITY_URL}/${cityId}/population-bootstrap/retry`, {
        method: "POST",
        body: request?.plannedPeopleCountOverride === undefined
            ? undefined
            : JSON.stringify(request),
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
