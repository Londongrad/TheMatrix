import type {CityProvisioningView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

export type ClassicCitySetupStepId = "scenario" | "profile" | "environment" | "launch";

export interface ClassicCitySetupDraftView {
    name: string;
    startSimTimeLocal: string;
    startSimTimeUtc?: string | null;
    speedMultiplier: string;
    climateZone: string;
    hemisphere: string;
    utcOffsetMinutes: string;
    generationSeed: string;
    sizeTier: string;
    urbanDensity: string;
    developmentLevel: string;
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
