import { API_AUTH_URL } from "../config";
import { request } from "../http";
import type {
  RegisterRequest,
  LoginRequest,
  LoginResponse,
  MeResponse,
  SessionInfo,
} from "./authTypes";
import { getOrCreateDeviceId, getOrCreateDeviceName } from "./deviceInfo";

export async function registerUser(data: RegisterRequest): Promise<void> {
  await request<void>(`${API_AUTH_URL}/register`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function loginUser(data: LoginRequest): Promise<LoginResponse> {
  const deviceId = getOrCreateDeviceId();
  const deviceName = getOrCreateDeviceName();

  // payload, который ждёт Gateway/Identity
  const payload = {
    ...data,
    deviceId,
    deviceName,
  };

  return await request<LoginResponse>(`${API_AUTH_URL}/login`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function refreshAuth(): Promise<LoginResponse> {
  const deviceId = getOrCreateDeviceId();

  const payload = {
    deviceId,
  };

  return await request<LoginResponse>(`${API_AUTH_URL}/refresh`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function logoutAuth(): Promise<void> {
  await request<void>(`${API_AUTH_URL}/logout`, {
    method: "POST",
  });
}

export async function getMe(token: string): Promise<MeResponse> {
  return await request<MeResponse>(`${API_AUTH_URL}/me`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}

// ---- работа с сессиями ----

export async function getSessions(token: string): Promise<SessionInfo[]> {
  return await request<SessionInfo[]>(`${API_AUTH_URL}/sessions`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}

export async function revokeSession(
  token: string,
  sessionId: string
): Promise<void> {
  await request<void>(`${API_AUTH_URL}/sessions/${sessionId}`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}

export async function revokeAllSessions(token: string): Promise<void> {
  await request<void>(`${API_AUTH_URL}/sessions`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}
