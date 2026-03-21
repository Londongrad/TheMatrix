export interface SimulationView {
    simulationId: string;
    hostId: string;
    hostKind: string;
    simulationKind: string;
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
