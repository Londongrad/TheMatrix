const NON_MAIN_LAYOUT_PREFIXES = [
    "/admin",
    "/userSettings",
    "/login",
    "/register",
    "/forgot-password",
    "/forbidden",
] as const;

type LocationState = {
    from?: unknown;
} | null | undefined;

function normalizePath(candidate: unknown): string | null {
    if (typeof candidate !== "string") {
        return null;
    }

    const trimmed = candidate.trim();

    if (!trimmed.startsWith("/")) {
        return null;
    }

    return trimmed;
}

export function isMainLayoutPath(candidate: unknown): candidate is string {
    const normalized = normalizePath(candidate);

    if (!normalized) {
        return false;
    }

    return !NON_MAIN_LAYOUT_PREFIXES.some((prefix) =>
        normalized === prefix || normalized.startsWith(`${prefix}/`),
    );
}

export function extractMainLayoutReturnPath(state: LocationState): string | null {
    const from = state?.from;
    return isMainLayoutPath(from) ? from : null;
}

export function resolveMainLayoutReturnPath(
    pathname: string,
    state?: LocationState,
): string {
    const stateFrom = extractMainLayoutReturnPath(state);

    if (stateFrom) {
        return stateFrom;
    }

    return isMainLayoutPath(pathname) ? pathname : "/";
}