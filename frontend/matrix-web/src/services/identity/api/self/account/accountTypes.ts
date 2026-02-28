// src/services/identity/api/account/accountTypes.ts
export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface ChangeAvatarResponse {
    avatarUrl: string | null;
}

export interface ProfileResponse {
    userId: string;
    email: string;
    username: string;
    avatarUrl: string | null;
    isEmailConfirmed: boolean;
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
