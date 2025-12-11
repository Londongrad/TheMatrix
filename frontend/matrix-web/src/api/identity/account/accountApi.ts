// src/api/account/accountApi.tsx
import { API_ACCOUNT_URL } from "@api/config";
import { apiRequest } from "@api/http";
import type {
  ChangeAvatarResponse,
  ChangePasswordRequest,
  ProfileResponse,
} from "./accountTypes";

export async function getProfile(token: string): Promise<ProfileResponse> {
  return await apiRequest<ProfileResponse>(`${API_ACCOUNT_URL}/profile`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}

// Если бек возвращает только 204 NoContent:
export async function changePassword(
  payload: ChangePasswordRequest,
  accessToken: string
): Promise<void> {
  await apiRequest<void>(`${API_ACCOUNT_URL}/password`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
}

export async function updateAvatar(
  file: File,
  accessToken: string
): Promise<ChangeAvatarResponse> {
  const formData = new FormData();
  formData.append("avatar", file);

  return await apiRequest<ChangeAvatarResponse>(`${API_ACCOUNT_URL}/avatar`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    body: formData,
  });
}
