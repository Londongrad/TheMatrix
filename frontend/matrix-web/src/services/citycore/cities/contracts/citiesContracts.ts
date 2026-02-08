//src/services/citycore/cities/contracts/citiesContracts.ts
export interface CityListItemView {
    cityId: string;
    name: string;
    simulationKind: string;
    status: string;
}

export interface CreateCityRequest {
    name: string;
    startSimTimeUtc: string;
    speedMultiplier: number;
    simulationKind?: string;
}

export interface CityCreatedView {
    cityId: string;
    populationBootstrapOperationId: string;
    simulationKind: string;
}

export interface CityView {
    cityId: string;
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
    createdAtUtc: string;
    archivedAtUtc?: string | null;
}

export interface RenameCityRequest {
    name: string;
}

export interface SimulationKindCatalogItemView {
    kind: string;
    displayName: string;
    description: string;
    supportsAutomaticPopulationBootstrap: boolean;
    isDefault: boolean;
}
