export interface CityListItemView {
    cityId: string;
    simulationId: string;
    name: string;
    simulationKind: string;
    status: string;
}

export interface CreateCityRequest {
    name: string;
    startSimTimeUtc: string;
    speedMultiplier: number;
    simulationKind?: string;
    climateZone?: string;
    hemisphere?: string;
    utcOffsetMinutes?: number;
    generationSeed?: string | null;
    sizeTier?: string | null;
    urbanDensity?: string | null;
    developmentLevel?: string | null;
    populationOccupancyProfile?: string | null;
    plannedPeopleCount?: number | null;
}

export interface CityPopulationBootstrapSummaryView {
    cityId: string;
    requestedPeopleCount: number;
    generatedPeopleCount: number;
    householdCount: number;
    housedHouseholdCount: number;
    homelessHouseholdCount: number;
    housedPeopleCount: number;
    homelessPeopleCount: number;
}

export interface CityPopulationBootstrapView {
    operationId: string;
    status: string;
    plannedPeopleCount?: number | null;
    residentialCapacity?: number | null;
    summary?: CityPopulationBootstrapSummaryView | null;
    failureCode?: string | null;
}

export interface CityProvisioningView {
    cityId: string;
    simulationKind: string;
    populationBootstrap: CityPopulationBootstrapView;
}

export interface CityProvisioningStatusView {
    cityId: string;
    status: string;
    populationBootstrapOperationId: string;
    populationBootstrapFailureCode?: string | null;
    populationBootstrapCompletedAtUtc?: string | null;
    populationBootstrapFailedAtUtc?: string | null;
}

export interface CityView {
    cityId: string;
    simulationId: string;
    name: string;
    simulationKind: string;
    status: string;
    climateZone: string;
    hemisphere: string;
    utcOffsetMinutes: number;
    generationSeed: string;
    sizeTier: string;
    urbanDensity: string;
    developmentLevel: string;
    populationOccupancyProfile: string;
    createdAtUtc: string;
    archivedAtUtc?: string | null;
    plannedPeopleCount?: number | null;
}

export interface RenameCityRequest {
    name: string;
}
