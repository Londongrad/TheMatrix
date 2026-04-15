export type SimulationCoreScenarioDefinition = {
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

export type ClassicCityWorkspaceSection =
    | "overview"
    | "dashboard"
    | "map"
    | "population"
    | "weather"
    | "simulation";

export type ClassicCityResidentSection =
    | "overview"
    | "relationships"
    | "career"
    | "education"
    | "health";

export const SIMULATIONCORE_SCENARIO_CATALOG_PATH = "/scenarios";
export const SIMULATIONCORE_NEW_SIMULATION_PATH = "/simulations/new";
export const CLASSIC_CITY_LIST_PATH = "/cities";
export const CLASSIC_CITY_DETAILS_PATH_PATTERN = "/cities/:cityId";
export const CLASSIC_CITY_RESIDENTS_PATH_PATTERN = "/cities/:cityId/residents";
export const CLASSIC_CITY_RESIDENT_DOSSIER_PATH_PATTERN = "/cities/:cityId/residents/:residentId";
export const CLASSIC_CITY_CIVIL_REGISTRY_PATH_PATTERN = "/cities/:cityId/civil-registry";
export const CLASSIC_CITY_EMPLOYMENT_PATH_PATTERN = "/cities/:cityId/employment";
export const CLASSIC_CITY_EDUCATION_PATH_PATTERN = "/cities/:cityId/education";
export const CLASSIC_CITY_SETUP_PATH = "/scenarios/classic-city/setup";
export const CLASSIC_CITY_SETUP_SESSION_PATH_PATTERN = "/scenarios/classic-city/setup/:sessionId";
export const CLASSIC_CITY_SETUP_PROVISIONING_PATH_PATTERN = "/scenarios/classic-city/setup/:sessionId/provisioning";
export const CLASSIC_CITY_PROVISIONING_PATH_PATTERN = "/cities/:cityId/provisioning";

export function getClassicCityDetailsPath(
    cityId: string,
    section?: ClassicCityWorkspaceSection,
): string {
    const basePath = `/cities/${cityId}`;

    if (!section) {
        return basePath;
    }

    return `${basePath}?tab=${encodeURIComponent(section)}`;
}

export function getClassicCityResidentsPath(cityId: string): string {
    return `/cities/${cityId}/residents`;
}

export function getClassicCityResidentDossierPath(
    cityId: string,
    residentId: string,
    section?: ClassicCityResidentSection,
): string {
    const basePath = `/cities/${cityId}/residents/${residentId}`;

    if (!section) {
        return basePath;
    }

    return `${basePath}?tab=${encodeURIComponent(section)}`;
}

export function getClassicCityCivilRegistryPath(cityId: string, residentId?: string): string {
    const basePath = `/cities/${cityId}/civil-registry`;

    if (!residentId) {
        return basePath;
    }

    return `${basePath}?residentId=${encodeURIComponent(residentId)}`;
}

export function getClassicCityEmploymentPath(cityId: string, residentId?: string): string {
    const basePath = `/cities/${cityId}/employment`;

    if (!residentId) {
        return basePath;
    }

    return `${basePath}?residentId=${encodeURIComponent(residentId)}`;
}

export function getClassicCityEducationPath(cityId: string, residentId?: string): string {
    const basePath = `/cities/${cityId}/education`;

    if (!residentId) {
        return basePath;
    }

    return `${basePath}?residentId=${encodeURIComponent(residentId)}`;
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

export const CLASSIC_CITY_SCENARIO: SimulationCoreScenarioDefinition = {
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
        "Topology, weather, and simulation clock managed by SimulationCore",
        "Population bootstrap reported back as a first-class launch result",
    ],
    detailsPathPattern: CLASSIC_CITY_DETAILS_PATH_PATTERN,
    buildDetailsPath: getClassicCityDetailsPath,
    buildProvisioningPath: getClassicCityProvisioningPath,
};

export const simulationCoreScenarioRegistry: SimulationCoreScenarioDefinition[] = [
    CLASSIC_CITY_SCENARIO,
];

export function getSimulationCoreScenario(kind: string): SimulationCoreScenarioDefinition | undefined {
    return simulationCoreScenarioRegistry.find((scenario) => scenario.kind === kind);
}
