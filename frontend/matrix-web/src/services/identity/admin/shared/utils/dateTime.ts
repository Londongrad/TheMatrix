export function formatAdminUtc(utc: string) {
    return utc?.replace("T", " ").replace("Z", "");
}

export function formatAdminVisitUtc(utc?: string | null) {
    return utc ? formatAdminUtc(utc) : "Never";
}
