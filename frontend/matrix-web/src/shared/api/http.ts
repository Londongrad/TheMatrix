// src/shared/api/http.ts
export class HttpError extends Error {
    status: number;
    payload?: unknown;

    constructor(status: number, message: string, payload?: unknown) {
        super(message);
        this.status = status;
        this.payload = payload;
    }
}

const MAX_PUBLIC_MESSAGE_LEN = 180;

function looksLikeStackTrace(text: string): boolean {
    const t = text.toLowerCase();
    return (
        t.includes("microsoft.aspnetcore") ||
        t.includes("system.") ||
        (t.includes("exception") && t.includes(" at ")) ||
        t.includes("developer exception page") ||
        t.includes("stack trace")
    );
}

function toSafeMessage(text: string): string {
    const firstLine = text.split("\n").find(Boolean)?.trim() ?? "";
    const short = firstLine.slice(0, MAX_PUBLIC_MESSAGE_LEN);
    return short || "Server error. Please try again.";
}

function isJsonContentType(contentType: string | null): boolean {
    if (!contentType) {
        return false;
    }

    const normalized = contentType.toLowerCase();
    return (
        normalized.includes("application/json") ||
        normalized.includes("+json")
    );
}

async function readResponseBody(response: Response): Promise<unknown> {
    const text = await response.text();

    if (!text) {
        return undefined;
    }

    if (isJsonContentType(response.headers.get("Content-Type"))) {
        return JSON.parse(text);
    }

    return text;
}

export interface AuthRefreshResult {
    accessToken: string | null;
    shouldLogout: boolean;
    error?: unknown;
}

type RefreshTokenFn = () => Promise<AuthRefreshResult>;
type LogoutFn = () => void;
type GetAccessTokenFn = () => string | null;
type ForbiddenFn = (info: { url: string }) => void;

let refreshTokenFn: RefreshTokenFn | null = null;
let logoutFn: LogoutFn | null = null;
let getAccessTokenFn: GetAccessTokenFn | null = null;
let forbiddenFn: ForbiddenFn | null = null;

// single-flight refresh (один refresh на всех)
let refreshInFlight: Promise<AuthRefreshResult> | null = null;

export function configureHttpAuth(options: {
    refreshToken: RefreshTokenFn;
    onLogout: LogoutFn;
    getAccessToken: GetAccessTokenFn;
    onForbidden?: ForbiddenFn;
}) {
    refreshTokenFn = options.refreshToken;
    logoutFn = options.onLogout;
    getAccessTokenFn = options.getAccessToken;
    forbiddenFn = options.onForbidden ?? null;
}

function addAuthHeaderIfMissing(
    options: RequestInit,
    token: string,
): RequestInit {
    const headers = new Headers(options.headers);
    if (!headers.has("Authorization")) {
        headers.set("Authorization", `Bearer ${token}`);
    }
    return {...options, headers};
}

function setAuthHeader(options: RequestInit, token: string): RequestInit {
    const headers = new Headers(options.headers);
    headers.set("Authorization", `Bearer ${token}`); // overwrite независимо от регистра
    return {...options, headers};
}

async function refreshOnce(): Promise<AuthRefreshResult> {
    if (!refreshTokenFn) {
        return {
            accessToken: null,
            shouldLogout: true,
        };
    }

    if (!refreshInFlight) {
        refreshInFlight = refreshTokenFn().finally(() => {
            refreshInFlight = null;
        });
    }

    return await refreshInFlight;
}

// Базовый helper над fetch
export async function request<T>(
    url: string,
    options: RequestInit = {},
): Promise<T> {
    const isFormData = options.body instanceof FormData;

    const headers = new Headers(options.headers);
    if (!isFormData && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }

    let response: Response;
    try {
        response = await fetch(url, {
            credentials: "include",
            ...options,
            headers,
        });
    } catch {
        throw new HttpError(
            0,
            "Не удалось подключиться к серверу. Попробуйте позже.",
        );
    }

    if (!response.ok) {
        const status = response.status;
        let message = `Request failed with status ${status}`;
        let payload: unknown = undefined;

        try {
            const text = await response.text();

            if (text) {
                // если пришёл JSON (ProblemDetails)
                if (isJsonContentType(response.headers.get("Content-Type"))) {
                    try {
                        const data = JSON.parse(text);
                        payload = data; // 👈 сохраняем JSON

                        if (typeof data === "string") {
                            message = data;
                        } else if (data.detail) {
                            message = data.detail;
                        } else if (data.title) {
                            message = data.title;
                        } else if (data.message) {
                            message = data.message;

                            const errors = data.errors;
                            if (errors && typeof errors === "object") {
                                const dict = errors as Record<string, string[]>;
                                const firstError = Object.values(dict).flat()[0];
                                if (firstError) message = firstError;
                            }
                        } else {
                            message = text;
                        }
                    } catch {
                        message = text;
                    }
                } else {
                    // НЕ показываем портянку пользователю
                    payload = text;

                    if (looksLikeStackTrace(text) || text.length > 500) {
                        message = "Server error. Please try again.";
                    } else {
                        message = toSafeMessage(text);
                    }
                }
            }
        } catch {
            // ignore
        }

        if (status === 415 && message === `Request failed with status ${status}`) {
            message =
                "Сервер не принимает такой формат файла. Попробуйте загрузить PNG или JPG размером до 2 МБ.";
        }

        // 👇 Передаём payload дальше
        throw new HttpError(status, message, payload);
    }

    if (response.status === 204) {
        // No Content
        return undefined as T;
    }

    return (await readResponseBody(response)) as T;
}

// 🔥 Глобальный клиент с авто-token + авто-refresh на 401 + /forbidden на 403
export async function apiRequest<T>(
    url: string,
    options: RequestInit = {},
    opts: {
        enableAuthRefresh?: boolean;
        attachAccessToken?: boolean;
    } = {},
): Promise<T> {
    const {enableAuthRefresh = true, attachAccessToken = true} = opts;

    // 1) Первая попытка: автоматически подставим access token, если его не передали в headers
    let firstOptions = options;

    if (attachAccessToken && getAccessTokenFn) {
        const token = getAccessTokenFn();
        if (token) {
            firstOptions = addAuthHeaderIfMissing(firstOptions, token);
        }
    }

    try {
        return await request<T>(url, firstOptions);
    } catch (err) {
        if (err instanceof HttpError) {
            // 401 → пробуем refresh, если включено
            if (err.status === 401 && enableAuthRefresh) {
                try {
                    const refreshResult = await refreshOnce();
                    const newToken = refreshResult.accessToken;

                    if (!newToken) {
                        if (refreshResult.shouldLogout) {
                            logoutFn?.();
                        }

                        if (refreshResult.error) {
                            throw refreshResult.error;
                        }

                        throw err;
                    }

                    // 2) Повторяем запрос уже с новым access token (перезаписываем Authorization)
                    const retryOptions = setAuthHeader(firstOptions, newToken);
                    return await request<T>(url, retryOptions);
                } catch (refreshError) {
                    if (refreshError === err) {
                        throw err;
                    }

                    if (refreshError instanceof HttpError &&
                        (refreshError.status === 401 || refreshError.status === 403)) {
                        logoutFn?.();
                        throw err;
                    }

                    throw refreshError;
                }
            }

            // 403 → SPA forbidden (без window.location.href)
            if (err.status === 403) {
                forbiddenFn?.({url});
                throw err; // можно оставить: пусть запрос считается неуспешным
            }
        }

        throw err;
    }
}
