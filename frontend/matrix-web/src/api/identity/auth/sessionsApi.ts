// sessions — уже через apiRequest (и токен не нужен)
import { API_AUTH_URL } from "@api/config";
import { apiRequest } from "@api/http";
import type { SessionInfo } from "./authTypes";

export async function getSessions(): Promise<SessionInfo[]> {
  return await apiRequest<SessionInfo[]>(`${API_AUTH_URL}/sessions`, {
    method: "GET",
  });
}

export async function revokeSession(sessionId: string): Promise<void> {
  await apiRequest<void>(`${API_AUTH_URL}/sessions/${sessionId}`, {
    method: "DELETE",
  });
}

export async function revokeAllSessions(): Promise<void> {
  await apiRequest<void>(`${API_AUTH_URL}/sessions`, {
    method: "DELETE",
  });
}
