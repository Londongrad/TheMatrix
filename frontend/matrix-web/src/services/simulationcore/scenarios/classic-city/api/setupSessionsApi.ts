import {apiRequest} from "@shared/api/http";
import {API_CLASSIC_CITY_SETUP_SESSIONS_URL} from "@shared/api/config";
import type {
    ClassicCitySetupSessionView,
    CreateClassicCitySetupSessionRequest,
    UpdateClassicCitySetupSessionRequest,
} from "@services/simulationcore/scenarios/classic-city/contracts/setupSessionContracts";

export function createClassicCitySetupSession(request: CreateClassicCitySetupSessionRequest) {
    return apiRequest<ClassicCitySetupSessionView>(API_CLASSIC_CITY_SETUP_SESSIONS_URL, {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function listClassicCitySetupSessions(signal?: AbortSignal) {
    return apiRequest<ClassicCitySetupSessionView[]>(API_CLASSIC_CITY_SETUP_SESSIONS_URL, {
        method: "GET",
        signal,
    });
}

export function getClassicCitySetupSession(sessionId: string, signal?: AbortSignal) {
    return apiRequest<ClassicCitySetupSessionView>(`${API_CLASSIC_CITY_SETUP_SESSIONS_URL}/${sessionId}`, {
        method: "GET",
        signal,
    });
}

export function updateClassicCitySetupSession(
    sessionId: string,
    request: UpdateClassicCitySetupSessionRequest,
    signal?: AbortSignal,
) {
    return apiRequest<ClassicCitySetupSessionView>(`${API_CLASSIC_CITY_SETUP_SESSIONS_URL}/${sessionId}`, {
        method: "PUT",
        body: JSON.stringify(request),
        signal,
    });
}

export function launchClassicCitySetupSession(sessionId: string) {
    return apiRequest<ClassicCitySetupSessionView>(`${API_CLASSIC_CITY_SETUP_SESSIONS_URL}/${sessionId}/launch`, {
        method: "POST",
    });
}
