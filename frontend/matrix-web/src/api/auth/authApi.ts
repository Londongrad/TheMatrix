import { API_AUTH_URL } from "../config";
import { request } from "../http";
import type {
  RegisterRequest,
  LoginRequest,
  LoginResponse,
  MeResponse,
} from "./authTypes";

export async function registerUser(data: RegisterRequest): Promise<void> {
  await request<void>(`${API_AUTH_URL}/register`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function loginUser(data: LoginRequest): Promise<LoginResponse> {
  return await request<LoginResponse>(`${API_AUTH_URL}/login`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function refreshAuth(): Promise<LoginResponse> {
  return await request<LoginResponse>(`${API_AUTH_URL}/refresh`, {
    method: "POST",
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
