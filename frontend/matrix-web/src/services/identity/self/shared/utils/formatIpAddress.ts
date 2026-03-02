export function formatIpAddress(value?: string | null): string {
    if (!value) {
        return "";
    }

    const normalized = value.trim().toLowerCase();
    if (
        normalized === "127.0.0.1" ||
        normalized === "::1" ||
        normalized === "::ffff:127.0.0.1"
    ) {
        return "localhost";
    }

    return value;
}
