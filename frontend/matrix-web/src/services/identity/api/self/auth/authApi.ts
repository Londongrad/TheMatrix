// src/services/identity/api/auth/authApi.ts
import {API_AUTH_URL} from "@shared/api/config";
import {request} from "@shared/api/http";
import type {
    ConfirmEmailRequest,
    ForgotPasswordRequest,
    LoginRequest,
    LoginResponse,
    RegisterRequest,
    ResetPasswordRequest,
    SendEmailConfirmationRequest,
} from "./authTypes";
import {getOrCreateDeviceId, getOrCreateDeviceName} from "./deviceInfo";

export async function registerUser(data: RegisterRequest): Promise<void> {
    await request<void>(`${API_AUTH_URL}/register`, {
        method: "POST",
        body: JSON.stringify(data),
    });
}

export async function loginUser(data: LoginRequest): Promise<LoginResponse> {
    const deviceId = getOrCreateDeviceId();
    const deviceName = getOrCreateDeviceName();

    const {login, password, rememberMe = true} = data;

    // payload, который ждёт Gateway/Identity
    const payload = {
        login,
        password,
        rememberMe,
        deviceId,
        deviceName,
    };

    return await request<LoginResponse>(`${API_AUTH_URL}/login`, {
        method: "POST",
        body: JSON.stringify(payload),
    });
}

export async function refreshAuth(): Promise<LoginResponse> {
    const deviceId = getOrCreateDeviceId();

    return await request<LoginResponse>(`${API_AUTH_URL}/refresh`, {
        method: "POST",
        body: JSON.stringify({deviceId}),
    });
}

export async function logoutAuth(): Promise<void> {
    await request<void>(`${API_AUTH_URL}/logout`, {method: "POST"});
}

export async function sendEmailConfirmationEmail(
    payload: SendEmailConfirmationRequest,
): Promise<void> {
    await request<void>(`${API_AUTH_URL}/email-confirmation/send`, {
        method: "POST",
        body: JSON.stringify(payload),
    });
}

export async function confirmEmail(
    payload: ConfirmEmailRequest,
): Promise<void> {
    await request<void>(`${API_AUTH_URL}/email-confirmation/confirm`, {
        method: "POST",
        body: JSON.stringify(payload),
    });
}

export async function forgotPassword(
    payload: ForgotPasswordRequest,
): Promise<void> {
    await request<void>(`${API_AUTH_URL}/password/forgot`, {
        method: "POST",
        body: JSON.stringify(payload),
    });
}

export async function resetPassword(
    payload: ResetPasswordRequest,
): Promise<void> {
    await request<void>(`${API_AUTH_URL}/password/reset`, {
        method: "POST",
        body: JSON.stringify({
            userId: payload.userId,
            token: payload.token,
            newPassword: payload.newPassword,
        }),
    });
}
