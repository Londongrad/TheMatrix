export interface SessionInfo {
    id: string;

    deviceId: string;
    deviceName: string;
    userAgent: string;
    ipAddress?: string | null;

    country?: string | null;
    region?: string | null;
    city?: string | null;

    createdAtUtc: string;
    lastUsedAtUtc?: string | null;
    refreshTokenExpiresAtUtc: string;

    isActive: boolean;
    isCurrent: boolean;
    isPersistent: boolean;

    location?: string | null;
}
