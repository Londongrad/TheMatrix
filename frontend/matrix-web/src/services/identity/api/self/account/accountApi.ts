// src/services/identity/api/account/accountApi.ts
import {API_ACCOUNT_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {
    ChangeAvatarResponse,
    ChangeEmailRequest,
    ChangeEmailResponse,
    ChangePasswordRequest,
    ChangeUsernameRequest,
    ChangeUsernameResponse,
    ProfileResponse,
    SecurityActivityItem,
} from "./accountTypes";

export async function fetchProfile(): Promise<ProfileResponse> {
    return await apiRequest<ProfileResponse>(`${API_ACCOUNT_URL}/profile`, {
        method: "GET",
    });
}

export async function fetchSecurityActivity(
    limit = 12,
): Promise<SecurityActivityItem[]> {
    return await apiRequest<SecurityActivityItem[]>(
        `${API_ACCOUNT_URL}/security-activity?limit=${limit}`,
        {
            method: "GET",
        },
    );
}

export async function changePassword(
    payload: ChangePasswordRequest,
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

export async function clearAvatar(): Promise<ChangeAvatarResponse> {
    return await apiRequest<ChangeAvatarResponse>(`${API_ACCOUNT_URL}/avatar`, {
        method: "DELETE",
    });
}

export async function changeUsername(
    payload: ChangeUsernameRequest,
): Promise<ChangeUsernameResponse> {
    return await apiRequest<ChangeUsernameResponse>(`${API_ACCOUNT_URL}/username`, {
        method: "PUT",
        body: JSON.stringify(payload),
    });
}

export async function changeEmail(
    payload: ChangeEmailRequest,
): Promise<ChangeEmailResponse> {
    return await apiRequest<ChangeEmailResponse>(`${API_ACCOUNT_URL}/email`, {
        method: "PUT",
        body: JSON.stringify(payload),
    });
}
