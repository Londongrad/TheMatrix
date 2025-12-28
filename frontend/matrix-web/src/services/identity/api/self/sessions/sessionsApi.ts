// src/services/identity/api/auth/sessionsApi.ts
import { API_SESSIONS_URL } from "@shared/api/config";
import { apiRequest } from "@shared/api/http";
import type { SessionInfo } from "./sessionsTypes";

export async function getSessions(): Promise<SessionInfo[]> {
  return await apiRequest<SessionInfo[]>(`${API_SESSIONS_URL}`, {
    method: "GET",
  });
}

export async function revokeSession(sessionId: string): Promise<void> {
  await apiRequest<void>(`${API_SESSIONS_URL}/${sessionId}`, {
    method: "DELETE",
  });
}

export async function revokeAllSessions(): Promise<void> {
  await apiRequest<void>(`${API_SESSIONS_URL}`, {
    method: "DELETE",
  });
}
