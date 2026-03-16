import {API_CITY_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {
    CityPopulationSummaryView
} from "@services/citycore/scenarios/classic-city/contracts/populationSummaryContracts";

export function getCityPopulationSummary(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityPopulationSummaryView>(`${API_CITY_URL}/${cityId}/population-summary`, {
        method: "GET",
        signal,
    });
}
