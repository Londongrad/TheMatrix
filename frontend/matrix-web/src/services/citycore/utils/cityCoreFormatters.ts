export function formatUtcIso(isoValue: string): string {
    const date = new Date(isoValue);
    if (Number.isNaN(date.getTime())) return isoValue;

    return `${date.toLocaleString("en-GB", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
        hour12: false,
        timeZone: "UTC",
    })} UTC`;
}

export function isGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}

export function toIsoUtcFromDatetimeLocal(value: string): string | null {
    if (!value) return null;

    const withSeconds = value.length === 16 ? `${value}:00` : value;
    const parsed = new Date(`${withSeconds}Z`);

    if (Number.isNaN(parsed.getTime())) return null;
    return parsed.toISOString();
}
