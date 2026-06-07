import {API_CLASSIC_CITY_CITIES_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {CityWeatherView} from "@services/simulationcore/scenarios/classic-city/contracts/weatherContracts";

export function getCityWeather(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityWeatherView>(`${API_CLASSIC_CITY_CITIES_URL}/${cityId}/weather`, {
        method: "GET",
        signal,
    });
}
