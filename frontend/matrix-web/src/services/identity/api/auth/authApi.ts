// src/api/auth/authApi.ts
import { API_AUTH_URL } from "@shared/api/config";
import { request } from "@shared/api/http";
import type { RegisterRequest, LoginRequest, LoginResponse } from "./authTypes";
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

  const { login, password, rememberMe = true } = data;

  // payload, который ждёт Gateway/Identity
  const payload = {
    login,
    password,
    rememberMe,
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

  return await request<LoginResponse>(`${API_AUTH_URL}/refresh`, {
    method: "POST",
    body: JSON.stringify({ deviceId }),
  });
}

export async function logoutAuth(): Promise<void> {
  await request<void>(`${API_AUTH_URL}/logout`, { method: "POST" });
}
