export type CityCoreScenarioDefinition = {
    kind: string;
    label: string;
    availabilityLabel: string;
    summary: string;
    description: string;
    setupPath: string;
    listPath: string;
    highlights: string[];
    detailsPathPattern?: string;
    buildDetailsPath?: (hostId: string) => string;
    buildProvisioningPath?: (hostId: string) => string;
};

export const CITYCORE_SCENARIO_CATALOG_PATH = "/scenarios";
export const CITYCORE_NEW_SIMULATION_PATH = "/simulations/new";
export const CLASSIC_CITY_LIST_PATH = "/cities";
export const CLASSIC_CITY_DETAILS_PATH_PATTERN = "/cities/:cityId";
export const CLASSIC_CITY_SETUP_PATH = "/scenarios/classic-city/setup";
export const CLASSIC_CITY_SETUP_SESSION_PATH_PATTERN = "/scenarios/classic-city/setup/:sessionId";
export const CLASSIC_CITY_SETUP_PROVISIONING_PATH_PATTERN = "/scenarios/classic-city/setup/:sessionId/provisioning";
export const CLASSIC_CITY_PROVISIONING_PATH_PATTERN = "/cities/:cityId/provisioning";

export function getClassicCityDetailsPath(cityId: string): string {
    return `/cities/${cityId}`;
}

export function getClassicCitySetupPath(): string {
    return CLASSIC_CITY_SETUP_PATH;
}

export function getClassicCitySetupSessionPath(sessionId: string): string {
    return `/scenarios/classic-city/setup/${sessionId}`;
}

export function getClassicCitySetupProvisioningPath(sessionId: string): string {
    return `/scenarios/classic-city/setup/${sessionId}/provisioning`;
}

export function getClassicCityProvisioningPath(cityId: string): string {
    return `/cities/${cityId}/provisioning`;
}

export const CLASSIC_CITY_SCENARIO: CityCoreScenarioDefinition = {
    kind: "ClassicCity",
    label: "Classic City",
    availabilityLabel: "Available now",
    summary: "City districts, weather, population bootstrap, and operator-facing simulation controls.",
    description:
        "The baseline city simulation flow. Configure the city profile, launch the initial world state, let Population bootstrap residents, and hand off the ready host to monitoring.",
    setupPath: CLASSIC_CITY_SETUP_PATH,
    listPath: CLASSIC_CITY_LIST_PATH,
    highlights: [
        "Route-level setup wizard instead of an inline sidebar form",
        "Topology, weather, and simulation clock managed by CityCore",
        "Population bootstrap reported back as a first-class launch result",
    ],
    detailsPathPattern: CLASSIC_CITY_DETAILS_PATH_PATTERN,
    buildDetailsPath: getClassicCityDetailsPath,
    buildProvisioningPath: getClassicCityProvisioningPath,
};

export const cityCoreScenarioRegistry: CityCoreScenarioDefinition[] = [
    CLASSIC_CITY_SCENARIO,
];

export function getCityCoreScenario(kind: string): CityCoreScenarioDefinition | undefined {
    return cityCoreScenarioRegistry.find((scenario) => scenario.kind === kind);
}
