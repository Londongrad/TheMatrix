// src/services/identity/api/auth/authTypes.ts
export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
}

export interface RegisterResponse {
  email: string;
  username: string;
}

export interface LoginRequest {
  login: string;
  password: string;
  rememberMe?: boolean;
}

// Это ответ от /api/auth/login и /api/auth/refresh
export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

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
