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

  isActive: boolean;

  location?: string | null;
}
