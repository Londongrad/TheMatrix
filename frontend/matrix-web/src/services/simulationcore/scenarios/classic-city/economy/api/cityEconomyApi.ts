import {apiRequest} from "@shared/api/http";
import {
    API_CLASSIC_CITY_ECONOMY_BUDGET_URL,
    API_CLASSIC_CITY_ECONOMY_BUSINESS_URL,
    API_CLASSIC_CITY_ECONOMY_HOUSEHOLD_ACCOUNTS_URL,
} from "@shared/api/config";
import type {
    CityBusinessDto,
    CityHouseholdAccountDto,
    CityOperationalBudgetPressureDto,
    EconomySummaryDto,
} from "@services/simulationcore/scenarios/classic-city/economy/api/cityEconomyContracts";

export function getCityBudgetSummary(cityId: string, signal?: AbortSignal) {
    return apiRequest<EconomySummaryDto>(
        `${API_CLASSIC_CITY_ECONOMY_BUDGET_URL}/cities/${cityId}/summary`,
        {method: "GET", signal},
    );
}

export function getCityOperationalBudgetPressure(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityOperationalBudgetPressureDto>(
        `${API_CLASSIC_CITY_ECONOMY_BUDGET_URL}/cities/${cityId}/operational-pressure`,
        {method: "GET", signal},
    );
}

export function getCityBusinesses(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityBusinessDto[]>(
        `${API_CLASSIC_CITY_ECONOMY_BUSINESS_URL}/cities/${cityId}`,
        {method: "GET", signal},
    );
}

export function getCityHouseholdAccounts(cityId: string, signal?: AbortSignal) {
    return apiRequest<CityHouseholdAccountDto[]>(
        `${API_CLASSIC_CITY_ECONOMY_HOUSEHOLD_ACCOUNTS_URL}/cities/${cityId}`,
        {method: "GET", signal},
    );
}
