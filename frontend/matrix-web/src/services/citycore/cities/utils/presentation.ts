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

export function formatCityStatusLabel(status?: string, archivedAtUtc?: string | null): string {
    if (!status && !archivedAtUtc) {
        return "Unknown";
    }

    return isArchivedCity(status, archivedAtUtc) ? "Archived" : "Active";
}

export function getCityStatusTone(
    status?: string,
    archivedAtUtc?: string | null,
): "active" | "archived" | "unknown" {
    if (!status && !archivedAtUtc) {
        return "unknown";
    }

    return isArchivedCity(status, archivedAtUtc) ? "archived" : "active";
}