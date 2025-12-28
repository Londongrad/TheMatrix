// src/services/identity/api/account/accountApi.ts
import { API_ACCOUNT_URL } from "@shared/api/config";
import { apiRequest } from "@shared/api/http";
import type {
  ChangeAvatarResponse,
  ChangePasswordRequest,
  ProfileResponse,
} from "./accountTypes";

export async function getProfile(): Promise<ProfileResponse> {
  return await apiRequest<ProfileResponse>(`${API_ACCOUNT_URL}/profile`, {
    method: "GET",
  });
}

export async function changePassword(
  payload: ChangePasswordRequest
): Promise<void> {
  await apiRequest<void>(`${API_ACCOUNT_URL}/password`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function updateAvatar(file: File): Promise<ChangeAvatarResponse> {
  const formData = new FormData();
  formData.append("avatar", file);

  return await apiRequest<ChangeAvatarResponse>(`${API_ACCOUNT_URL}/avatar`, {
    method: "PUT",
    body: formData,
  });
}
