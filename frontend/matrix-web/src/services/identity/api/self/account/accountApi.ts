// src/services/identity/api/account/accountApi.ts
import {API_ACCOUNT_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {CursorPagedResult} from "@shared/lib/paging/cursorPagingTypes";
import type {
    ChangeAvatarResponse,
    ChangeDisplayNameRequest,
    ChangeDisplayNameResponse,
    ChangeEmailRequest,
    ChangeEmailResponse,
    ChangePasswordRequest,
    ChangeUsernameRequest,
    ChangeUsernameResponse,
    DeleteAccountRequest,
    ProfileResponse,
    SecurityActivityItem
} from "./accountTypes";

export async function fetchProfile(): Promise<ProfileResponse> {
    return await apiRequest<ProfileResponse>(`${API_ACCOUNT_URL}/profile`, {
        method: "GET",
    });
}

export async function fetchSecurityActivityFeed(
    cursor: string | null = null,
    pageSize = 12,
    signal?: AbortSignal,
): Promise<CursorPagedResult<SecurityActivityItem>> {
    const params = new URLSearchParams({
        pageSize: pageSize.toString(),
    });

    if (cursor) {
        params.set("cursor", cursor);
    }

    return await apiRequest<CursorPagedResult<SecurityActivityItem>>(
        `${API_ACCOUNT_URL}/security-activity?${params.toString()}`,
        {
            method: "GET",
            signal,
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

export async function changeDisplayName(
    payload: ChangeDisplayNameRequest,
): Promise<ChangeDisplayNameResponse> {
    return await apiRequest<ChangeDisplayNameResponse>(`${API_ACCOUNT_URL}/display-name`, {
        method: "PUT",
        body: JSON.stringify(payload),
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

export async function resendPendingEmailChange(): Promise<void> {
    await apiRequest<void>(`${API_ACCOUNT_URL}/email/pending/resend`, {
        method: "POST",
    });
}

export async function cancelPendingEmailChange(): Promise<void> {
    await apiRequest<void>(`${API_ACCOUNT_URL}/email/pending`, {
        method: "DELETE",
    });
}

export async function deleteAccount(
    payload: DeleteAccountRequest,
): Promise<void> {
    await apiRequest<void>(`${API_ACCOUNT_URL}/delete`, {
        method: "POST",
        body: JSON.stringify(payload),
    });
}
