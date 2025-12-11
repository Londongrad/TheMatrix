// src/api/account/accountTypes.ts
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ChangeAvatarResponse {
  avatarUrl: string;
}

export interface ProfileResponse {
  userId: string;
  email: string;
  username: string;
  avatarUrl: string | null;
}
