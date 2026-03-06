import {apiRequest} from "@shared/api/http";
import {API_CITYCORE_DASHBOARD_URL} from "@shared/api/config";
import type {CityOperationsDashboardView} from "@services/citycore/dashboard/api/dashboardTypes";

export function getCityOperationsDashboard(signal?: AbortSignal) {
    return apiRequest<CityOperationsDashboardView>(API_CITYCORE_DASHBOARD_URL, {
        method: "GET",
        signal,
    });
}
