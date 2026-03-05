const DEFAULT_ADMIN_LOCALE = "en";

function resolveAdminLocale() {
    if (typeof document !== "undefined" && document.documentElement.lang) {
        return document.documentElement.lang;
    }

    if (typeof navigator !== "undefined") {
        return (
            navigator.languages?.find(Boolean) ??
            navigator.language ??
            DEFAULT_ADMIN_LOCALE
        );
    }

    return DEFAULT_ADMIN_LOCALE;
}

function normalizeIsoFallback(utc: string) {
    return utc
        .replace("T", " ")
        .replace("Z", "")
        .replace(/\.\d+/, "");
}

function parseUtc(utc?: string | null) {
    if (!utc) {
        return null;
    }

    const parsed = new Date(utc);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function formatRelativeTime(value: number, unit: Intl.RelativeTimeFormatUnit) {
    const formatter = new Intl.RelativeTimeFormat(resolveAdminLocale(), {
        numeric: "auto",
    });

    return formatter.format(value, unit);
}

export function formatAdminUtc(utc?: string | null) {
    if (!utc) {
        return "--";
    }

    const parsed = parseUtc(utc);

    if (!parsed) {
        return normalizeIsoFallback(utc);
    }

    return new Intl.DateTimeFormat(resolveAdminLocale(), {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
    }).format(parsed);
}

export function formatAdminVisitUtc(utc?: string | null) {
    return utc ? formatAdminUtc(utc) : "Never";
}

export function formatAdminRelativeVisit(utc?: string | null) {
    if (!utc) {
        return "Never";
    }

    const visitDate = parseUtc(utc);

    if (!visitDate) {
        return formatAdminUtc(utc);
    }

    const diffSeconds = Math.round((visitDate.getTime() - Date.now()) / 1000);
    const absSeconds = Math.abs(diffSeconds);

    if (absSeconds < 45) {
        return formatRelativeTime(0, "second");
    }

    const diffMinutes = Math.round(diffSeconds / 60);

    if (Math.abs(diffMinutes) < 60) {
        return formatRelativeTime(diffMinutes, "minute");
    }

    const diffHours = Math.round(diffMinutes / 60);

    if (Math.abs(diffHours) < 24) {
        return formatRelativeTime(diffHours, "hour");
    }

    const diffDays = Math.round(diffHours / 24);

    if (Math.abs(diffDays) < 7) {
        return formatRelativeTime(diffDays, "day");
    }

    const diffWeeks = Math.round(diffDays / 7);

    if (Math.abs(diffWeeks) < 5) {
        return formatRelativeTime(diffWeeks, "week");
    }

    const diffMonths = Math.round(diffDays / 30);

    if (Math.abs(diffMonths) < 12) {
        return formatRelativeTime(diffMonths, "month");
    }

    const diffYears = Math.round(diffDays / 365);
    return formatRelativeTime(diffYears, "year");
}
