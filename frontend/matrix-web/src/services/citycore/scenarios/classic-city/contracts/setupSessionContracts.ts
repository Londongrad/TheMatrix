import type {CityProvisioningView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

export type ClassicCitySetupStepId = "scenario" | "profile" | "environment" | "population" | "launch";
export type ClassicCityPopulationOccupancyProfile = "Light" | "Balanced" | "High";
export type ClassicCityPopulationTargetMode = "Random" | "Preset1K" | "Preset10K" | "Preset100K" | "Manual";
export type ClassicCityInitialWeatherMode = "Random" | "Manual";
export type ClassicCityInitialWeatherType =
    | "Clear"
    | "Overcast"
    | "Rain"
    | "Snow"
    | "Storm"
    | "Fog"
    | "Windy"
    | "Heatwave"
    | "ColdSnap";
export type ClassicCityInitialWeatherSeverity = "Calm" | "Mild" | "Moderate" | "Severe" | "Extreme";

export interface ClassicCitySetupDraftView {
    name: string;
    startSimTimeLocal: string;
    startSimTimeUtc?: string | null;
    speedMultiplier: string;
    climateZone: string;
    hemisphere: string;
    utcOffsetMinutes: string;
    generationSeed: string;
    initialWeatherMode: ClassicCityInitialWeatherMode;
    initialWeatherType: ClassicCityInitialWeatherType;
    initialWeatherSeverity: ClassicCityInitialWeatherSeverity;
    initialWeatherTemperatureC: string;
    populationTargetMode: ClassicCityPopulationTargetMode;
    sizeTier: string;
    urbanDensity: string;
    developmentLevel: string;
    populationOccupancyProfile: ClassicCityPopulationOccupancyProfile;
    plannedPeopleCount: string;
}

export interface CreateClassicCitySetupSessionRequest {
    currentStepId: ClassicCitySetupStepId;
    draft: ClassicCitySetupDraftView;
}

export interface UpdateClassicCitySetupSessionRequest {
    currentStepId: ClassicCitySetupStepId;
    draft: ClassicCitySetupDraftView;
}

export interface ClassicCitySetupSessionView {
    sessionId: string;
    scenarioKind: string;
    status: string;
    currentStepId: ClassicCitySetupStepId;
    draft: ClassicCitySetupDraftView;
    cityId?: string | null;
    simulationKind?: string | null;
    provisioning?: CityProvisioningView | null;
    failureCode?: string | null;
    failureMessage?: string | null;
    createdAtUtc: string;
    updatedAtUtc: string;
    launchQueuedAtUtc?: string | null;
    startedAtUtc?: string | null;
    completedAtUtc?: string | null;
}
