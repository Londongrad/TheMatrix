// src/services/identity/api/auth/authTypes.ts
export interface RegisterRequest {
    email: string;
    username: string;
    password: string;
    confirmPassword: string;
}

export interface RegisterResponse {
    email: string;
    username: string;
}

export interface SendEmailConfirmationRequest {
    email: string;
}

export interface ConfirmEmailRequest {
    userId: string;
    token: string;
}

export interface ForgotPasswordRequest {
    email: string;
}

export interface ResetPasswordRequest {
    userId: string;
    token: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface LoginRequest {
    login: string;
    password: string;
    rememberMe?: boolean;
}

// Это ответ от /api/auth/login и /api/auth/refresh
export interface LoginResponse {
    accessToken: string;
    tokenType: string;
    expiresIn: number;
    refreshToken: string;
    refreshTokenExpiresAtUtc: string;
}
