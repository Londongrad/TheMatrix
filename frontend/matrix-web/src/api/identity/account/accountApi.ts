import { API_ACCOUNT_URL } from "@api/config";
import { apiRequest } from "@api/http";
import type { ChangePasswordRequest } from "./accountTypes";

// Если бек возвращает только 204 NoContent:
export async function changePassword(
  payload: ChangePasswordRequest,
  accessToken: string
): Promise<void> {
  await apiRequest<void>(`${API_ACCOUNT_URL}/password`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  });
}

export async function updateAvatar(
  file: File,
  accessToken: string
): Promise<void> {
  const formData = new FormData();
  formData.append("avatar", file);

  await apiRequest<void>(`${API_ACCOUNT_URL}/avatar`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    body: formData,
  });
}
