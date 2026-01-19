export type ClockState = "Running" | "Paused" | string;

export interface BootstrapCityResponseDto {
    cityId: string;
}

export interface CityClockResponseDto {
    cityId: string;
    simTimeUtc: string;
    tickId: number;
    speed: number;
    state: ClockState;
}

export interface SetCityClockSpeedRequestDto {
    multiplier: number;
}

export interface JumpCityClockRequestDto {
    newSimTimeUtc: string;
}
