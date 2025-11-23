import { API_BASE_URL } from "../config";

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
}

export interface LoginRequest {
  login: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface MeResponse {
  userId: string;
  email: string;
  username: string;
}

// helper для fetch с JSON
async function request<T>(url: string, options: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {}),
    },
    ...options,
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed with status ${response.status}`);
  }

  return (await response.json()) as T;
}

export async function registerUser(data: RegisterRequest): Promise<void> {
  await request<void>(`${API_BASE_URL}/api/auth/register`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function loginUser(data: LoginRequest): Promise<LoginResponse> {
  return await request<LoginResponse>(`${API_BASE_URL}/api/auth/login`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export async function refreshAuth(
  refreshToken: string
): Promise<LoginResponse> {
  return await request<LoginResponse>(`${API_BASE_URL}/api/auth/refresh`, {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
}

export async function logoutAuth(refreshToken: string): Promise<void> {
  await request<void>(`${API_BASE_URL}/api/auth/logout`, {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
}

export async function getMe(token: string): Promise<MeResponse> {
  return await request<MeResponse>(`${API_BASE_URL}/api/auth/me`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}
