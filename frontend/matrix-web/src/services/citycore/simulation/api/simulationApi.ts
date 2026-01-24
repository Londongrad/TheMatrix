import { apiRequest } from "@shared/api/http";
import { API_CITY_URL } from "@shared/api/config";
import type {
    SimulationView,
    JumpClockRequest,
    SetSpeedRequest,
} from "@services/citycore/simulation/contracts/simulationContracts";

function simulationBase(cityId: string) {
    return `${API_CITY_URL}/${cityId}/simulation`;
}

export function getSimulationClock(cityId: string, signal?: AbortSignal) {
    return apiRequest<SimulationView>(simulationBase(cityId), { method: "GET", signal });
}

export function pauseSimulation(cityId: string) {
    return apiRequest<void>(`${simulationBase(cityId)}/pause`, { method: "POST" });
}

export function resumeSimulation(cityId: string) {
    return apiRequest<void>(`${simulationBase(cityId)}/resume`, { method: "POST" });
}

export function setSimulationSpeed(cityId: string, request: SetSpeedRequest) {
    return apiRequest<void>(`${simulationBase(cityId)}/speed`, {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function jumpSimulationClock(cityId: string, request: JumpClockRequest) {
    return apiRequest<void>(`${simulationBase(cityId)}/jump`, {
        method: "POST",
        body: JSON.stringify(request),
    });
}
