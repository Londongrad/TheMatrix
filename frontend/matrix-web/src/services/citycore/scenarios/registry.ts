export type CityCoreScenarioDefinition = {
    kind: string;
    label: string;
    availabilityLabel: string;
    summary: string;
    description: string;
    listPath: string;
    detailsPathPattern?: string;
    buildDetailsPath?: (hostId: string) => string;
};

export const CITYCORE_SCENARIO_CATALOG_PATH = "/scenarios";
export const CITYCORE_NEW_SIMULATION_PATH = "/simulations/new";
export const CLASSIC_CITY_LIST_PATH = "/cities";
export const CLASSIC_CITY_DETAILS_PATH_PATTERN = "/cities/:cityId";

export function getClassicCityDetailsPath(cityId: string): string {
    return `/cities/${cityId}`;
}

export const CLASSIC_CITY_SCENARIO: CityCoreScenarioDefinition = {
    kind: "ClassicCity",
    label: "Classic City",
    availabilityLabel: "Available now",
    summary: "City districts, weather, population bootstrap, and operator-facing simulation controls.",
    description:
        "The baseline scenario for running city-backed simulations. It keeps the current production workflow intact while leaving room for additional scenario modules later.",
    listPath: CLASSIC_CITY_LIST_PATH,
    detailsPathPattern: CLASSIC_CITY_DETAILS_PATH_PATTERN,
    buildDetailsPath: getClassicCityDetailsPath,
};

export const cityCoreScenarioRegistry: CityCoreScenarioDefinition[] = [
    CLASSIC_CITY_SCENARIO,
];

export function getCityCoreScenario(kind: string): CityCoreScenarioDefinition | undefined {
    return cityCoreScenarioRegistry.find((scenario) => scenario.kind === kind);
}
