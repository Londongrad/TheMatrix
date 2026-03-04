export function formatCityShortId(cityId: string, head = 8, tail = 4): string {
    if (!cityId) {
        return "Unknown";
    }

    if (cityId.length <= head + tail + 1) {
        return cityId;
    }

    return `${cityId.slice(0, head)}...${cityId.slice(-tail)}`;
}

export function isArchivedCity(status?: string, archivedAtUtc?: string | null): boolean {
    return Boolean(archivedAtUtc) || status?.trim().toLowerCase() === "archived";
}

export type CityStatusTone = "active" | "archived" | "provisioning" | "failed" | "unknown";

function normalizeStatus(status?: string): string {
    return status?.trim().toLowerCase() ?? "";
}

function humanizeLifecycleValue(value: string): string {
    return value
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .replace(/[_-]+/g, " ")
        .trim()
        .replace(/\b\w/g, (match) => match.toUpperCase());
}

export function formatCityStatusLabel(status?: string, archivedAtUtc?: string | null): string {
    if (!status && !archivedAtUtc) {
        return "Unknown";
    }

    if (isArchivedCity(status, archivedAtUtc)) {
        return "Archived";
    }

    switch (normalizeStatus(status)) {
        case "provisioning":
            return "Provisioning";
        case "provisioningfailed":
            return "Provisioning failed";
        case "active":
            return "Active";
        default:
            return status ? humanizeLifecycleValue(status) : "Unknown";
    }
}

export function getCityStatusTone(
    status?: string,
    archivedAtUtc?: string | null,
): CityStatusTone {
    if (!status && !archivedAtUtc) {
        return "unknown";
    }

    if (isArchivedCity(status, archivedAtUtc)) {
        return "archived";
    }

    switch (normalizeStatus(status)) {
        case "provisioning":
            return "provisioning";
        case "provisioningfailed":
            return "failed";
        case "active":
            return "active";
        default:
            return "unknown";
    }
}

export function describeCityLifecycle(
    status?: string,
    archivedAtUtc?: string | null,
    surface: "registry" | "workspace" = "registry",
): string {
    const tone = getCityStatusTone(status, archivedAtUtc);

    switch (tone) {
        case "archived":
            return surface === "workspace"
                ? "This record is archived and stays available for audit, cleanup, and historical review. Simulation mutations remain locked."
                : "Read-only record retained for audit and cleanup. Simulation mutations stay locked while the city remains archived.";
        case "provisioning":
            return surface === "workspace"
                ? "Launch is still running. Review the provisioning handoff until downstream bootstrap reports a final outcome."
                : "Launch is still running. Open the provisioning handoff to watch bootstrap progress before entering monitoring.";
        case "failed":
            return surface === "workspace"
                ? "Provisioning failed. Resolve or retry downstream bootstrap from the handoff screen before using live monitoring."
                : "Provisioning finished with a failure. Open the handoff to inspect the bootstrap error and retry it.";
        case "active":
            return surface === "workspace"
                ? "Ready city workspace with lifecycle controls, monitoring panels, and simulation management."
                : "Ready city workspace with simulation control, weather monitoring, and population review.";
        default:
            return "Backend returned a lifecycle state that the frontend does not recognise yet. Review the raw status before taking action.";
    }
}

export function formatSimulationKindLabel(simulationKind?: string): string {
    if (!simulationKind) {
        return "Unknown";
    }

    return simulationKind
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .trim();
}

export function formatProvisioningFailureCode(value?: string | null): string {
    if (!value) {
        return "Unknown failure";
    }

    return value
        .split(/[._-]+/)
        .filter(Boolean)
        .flatMap((segment) => segment.replace(/([a-z0-9])([A-Z])/g, "$1 $2").split(/\s+/))
        .filter(Boolean)
        .map((segment) => segment.charAt(0) + segment.slice(1).toLowerCase())
        .join(" ");
}
