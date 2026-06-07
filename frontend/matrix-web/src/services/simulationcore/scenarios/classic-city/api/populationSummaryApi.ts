import {API_CLASSIC_CITY_CITIES_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {
    CityPopulationSummaryView
} from "@services/simulationcore/scenarios/classic-city/contracts/populationSummaryContracts";

export function getCityPopulationSummary(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityPopulationSummaryView>(`${API_CLASSIC_CITY_CITIES_URL}/${cityId}/population-summary`, {
        method: "GET",
        signal,
    });
}
