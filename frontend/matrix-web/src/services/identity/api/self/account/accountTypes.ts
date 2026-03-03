// src/services/identity/api/account/accountTypes.ts
export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface ChangeAvatarResponse {
    avatarUrl: string | null;
}

export interface ChangeDisplayNameRequest {
    displayName: string | null;
}

export interface ChangeDisplayNameResponse {
    displayName: string | null;
}

export interface ChangeUsernameRequest {
    username: string;
    currentPassword: string;
}

export interface ChangeUsernameResponse {
    username: string;
}

export interface ChangeEmailRequest {
    newEmail: string;
    currentPassword: string;
}

export interface ChangeEmailResponse {
    pendingEmail: string;
}

export interface DeleteAccountRequest {
    currentPassword: string;
}

export interface ProfileResponse {
    userId: string;
    email: string;
    pendingEmail: string | null;
    username: string;
    displayName: string | null;
    avatarUrl: string | null;
    isEmailConfirmed: boolean;
    createdAtUtc: string;
    emailConfirmedAtUtc: string | null;
    effectivePermissions: string[];
    permissionsVersion: number;
}

export interface SecurityActivityItem {
    eventType: string;
    isSuccessful: boolean;
    occurredAtUtc: string;
    ipAddress?: string | null;
    userAgent?: string | null;
    deviceId?: string | null;
    deviceName?: string | null;
    details?: string | null;
}
