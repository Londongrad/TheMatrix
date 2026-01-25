//src/services/citycore/simulation/contracts/simulationContracts.tsx
export interface SimulationView {
    cityId: string;
    simTimeUtc: string;
    tickId: number;
    speed: number;
    state: string;
}

export interface SetSpeedRequest {
    multiplier: number;
}

export interface JumpClockRequest {
    newSimTimeUtc: string;
}
