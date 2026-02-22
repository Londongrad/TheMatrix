export function formatAdminUtc(utc: string) {
    return utc?.replace("T", " ").replace("Z", "");
}

export function formatAdminVisitUtc(utc?: string | null) {
    return utc ? formatAdminUtc(utc) : "Never";
}

function formatRelativeTime(
    value: number,
    unit: Intl.RelativeTimeFormatUnit
) {
    const formatter = new Intl.RelativeTimeFormat("en", {
        numeric: "auto",
    });

    return formatter.format(value, unit);
}

export function formatAdminRelativeVisit(utc?: string | null) {
    if (!utc)
        return "Never";

    const visitDate = new Date(utc);

    if (Number.isNaN(visitDate.getTime()))
        return formatAdminUtc(utc);

    const diffMs = visitDate.getTime() - Date.now();
    const diffMinutes = Math.round(diffMs / 60000);

    if (Math.abs(diffMinutes) < 1)
        return "Just now";

    if (Math.abs(diffMinutes) < 60)
        return formatRelativeTime(diffMinutes, "minute");

    const diffHours = Math.round(diffMinutes / 60);

    if (Math.abs(diffHours) < 24)
        return formatRelativeTime(diffHours, "hour");

    const diffDays = Math.round(diffHours / 24);

    if (Math.abs(diffDays) < 7)
        return formatRelativeTime(diffDays, "day");

    const diffWeeks = Math.round(diffDays / 7);

    if (Math.abs(diffWeeks) < 5)
        return formatRelativeTime(diffWeeks, "week");

    return formatAdminUtc(utc);
}
