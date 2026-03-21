export function pad2(value: number): string {
    return String(value).padStart(2, "0");
}

export function getNowLocalDateTimeInputValue(date = new Date()): string {
    const year = date.getFullYear();
    const month = pad2(date.getMonth() + 1);
    const day = pad2(date.getDate());
    const hours = pad2(date.getHours());
    const minutes = pad2(date.getMinutes());

    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

export function localDateTimeToUtcIso(value: string): string | null {
    if (!value.trim()) {
        return null;
    }

    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
        return null;
    }

    return parsed.toISOString();
}
