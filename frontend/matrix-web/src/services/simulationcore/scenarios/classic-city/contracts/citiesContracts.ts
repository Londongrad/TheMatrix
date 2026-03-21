export interface CityListItemView {
    cityId: string;
    simulationId: string;
    name: string;
    simulationKind: string;
    status: string;
    createdAtUtc: string;
    populationBootstrapCompletedAtUtc?: string | null;
    populationBootstrapFailedAtUtc?: string | null;
    populationBootstrapFailureCode?: string | null;
    archivedAtUtc?: string | null;
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
    initialWeatherMode?: string | null;
    initialWeatherType?: string | null;
    initialWeatherSeverity?: string | null;
    initialWeatherTemperatureC?: number | null;
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

export interface CityDashboardMetricView {
    key: string;
    label: string;
    description: string;
    valueKind: string;
    currentValue: number;
    deltaYesterday?: number | null;
    deltaMonth?: number | null;
    deltaYear?: number | null;
}

export interface CityDashboardActivityEventView {
    activityEventId: string;
    currentDate: string;
    occurredAtUtc: string;
    eventType: string;
    source: string;
    severity: string;
    title: string;
    summary: string;
    primaryResidentId?: string | null;
    secondaryResidentId?: string | null;
}

export interface CityDashboardView {
    cityId: string;
    currentDate: string;
    generatedAtUtc: string;
    metrics: CityDashboardMetricView[];
    recentEvents: CityDashboardActivityEventView[];
}

export interface RenameCityRequest {
    name: string;
}

export interface RetryPopulationBootstrapRequest {
    plannedPeopleCountOverride?: number | null;
}
