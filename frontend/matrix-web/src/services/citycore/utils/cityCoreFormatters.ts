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

export function isEmptyGuid(value: string): boolean {
    return value.trim().toLowerCase() === "00000000-0000-0000-0000-000000000000";
}

export function generateGuid(): string {
    if (typeof globalThis.crypto?.randomUUID === "function") {
        return globalThis.crypto.randomUUID();
    }

    // Fallback for environments without crypto.randomUUID
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (char) => {
        const random = Math.floor(Math.random() * 16);
        const value = char === "x" ? random : (random & 0x3) | 0x8;
        return value.toString(16);
    });
}

export function toIsoUtcFromDatetimeLocal(value: string): string | null {
    if (!value) return null;

    const withSeconds = value.length === 16 ? `${value}:00` : value;
    const parsed = new Date(`${withSeconds}Z`);

    if (Number.isNaN(parsed.getTime())) return null;
    return parsed.toISOString();
}
