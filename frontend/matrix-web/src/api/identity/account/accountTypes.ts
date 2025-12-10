export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ChangeAvatarResponse {
  avatarUrl: string;
}
