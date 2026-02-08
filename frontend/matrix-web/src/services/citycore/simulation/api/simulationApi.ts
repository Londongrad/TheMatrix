import {apiRequest} from "@shared/api/http";
import {API_SIMULATION_URL} from "@shared/api/config";
import type {
    JumpClockRequest,
    SetSpeedRequest,
    SimulationView,
} from "@services/citycore/simulation/contracts/simulationContracts";

function simulationBase(simulationId: string) {
    return `${API_SIMULATION_URL}/${simulationId}`;
}

export function getSimulationClock(simulationId: string, signal?: AbortSignal) {
    return apiRequest<SimulationView>(simulationBase(simulationId), {method: "GET", signal});
}

export function pauseSimulation(simulationId: string) {
    return apiRequest<void>(`${simulationBase(simulationId)}/pause`, {method: "POST"});
}

export function resumeSimulation(simulationId: string) {
    return apiRequest<void>(`${simulationBase(simulationId)}/resume`, {method: "POST"});
}

export function setSimulationSpeed(simulationId: string, request: SetSpeedRequest) {
    return apiRequest<void>(`${simulationBase(simulationId)}/speed`, {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function jumpSimulationClock(simulationId: string, request: JumpClockRequest) {
    return apiRequest<void>(`${simulationBase(simulationId)}/jump`, {
        method: "POST",
        body: JSON.stringify(request),
    });
}
