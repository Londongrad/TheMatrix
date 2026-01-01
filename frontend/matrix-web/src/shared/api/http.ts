// src/api/http.ts
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

type RefreshTokenFn = () => Promise<string | null>;
type LogoutFn = () => void;
type GetAccessTokenFn = () => string | null;
type ForbiddenFn = (info: { url: string }) => void;

let refreshTokenFn: RefreshTokenFn | null = null;
let logoutFn: LogoutFn | null = null;
let getAccessTokenFn: GetAccessTokenFn | null = null;
let forbiddenFn: ForbiddenFn | null = null;

// single-flight refresh (один refresh на всех)
let refreshInFlight: Promise<string | null> | null = null;

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

function normalizeHeaders(init?: HeadersInit): Record<string, string> {
  const headers = new Headers(init);
  const obj: Record<string, string> = {};
  headers.forEach((value, key) => {
    obj[key] = value;
  });
  return obj;
}

function addAuthHeaderIfMissing(
  options: RequestInit,
  token: string
): RequestInit {
  const hasAuth = new Headers(options.headers).has("Authorization");
  if (hasAuth) return options;

  const headersObj = normalizeHeaders(options.headers);
  headersObj["Authorization"] = `Bearer ${token}`;

  return { ...options, headers: headersObj };
}

function setAuthHeader(options: RequestInit, token: string): RequestInit {
  const headersObj = normalizeHeaders(options.headers);
  headersObj["Authorization"] = `Bearer ${token}`;
  return { ...options, headers: headersObj };
}

async function refreshOnce(): Promise<string | null> {
  if (!refreshTokenFn) return null;

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
  options: RequestInit = {}
): Promise<T> {
  let response: Response;

  // 👇 добавляем определение, FormData это или нет
  const isFormData = options.body instanceof FormData;

  // если FormData → НЕ ставим Content-Type
  const baseHeaders: HeadersInit = options.headers ?? {};
  const headers: HeadersInit = isFormData
    ? baseHeaders
    : {
        "Content-Type": "application/json",
        ...baseHeaders,
      };

  try {
    response = await fetch(url, {
      credentials: "include",
      ...options,
      headers,
    });
  } catch {
    // Сетевая ошибка / сервер упал
    throw new HttpError(
      0,
      "Не удалось подключиться к серверу. Попробуйте позже."
    );
  }

  if (!response.ok) {
    const status = response.status;
    let message = `Request failed with status ${status}`;
    let payload: unknown = undefined;

    try {
      const contentType = response.headers.get("Content-Type") || "";
      const text = await response.text();

      if (text) {
        // если пришёл JSON (ProblemDetails)
        if (contentType.includes("application/json")) {
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

  // предполагаем JSON-ответ
  return (await response.json()) as T;
}

// 🔥 Глобальный клиент с авто-token + авто-refresh на 401 + /forbidden на 403
export async function apiRequest<T>(
  url: string,
  options: RequestInit = {},
  opts: {
    enableAuthRefresh?: boolean;
    attachAccessToken?: boolean;
  } = {}
): Promise<T> {
  const { enableAuthRefresh = true, attachAccessToken = true } = opts;

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
          const newToken = await refreshOnce();

          if (!newToken) {
            logoutFn?.();
            throw err;
          }

          // 2) Повторяем запрос уже с новым access token (перезаписываем Authorization)
          const retryOptions = setAuthHeader(options, newToken);
          return await request<T>(url, retryOptions);
        } catch {
          logoutFn?.();
          throw err;
        }
      }

      // 403 → SPA forbidden (без window.location.href)
      if (err.status === 403) {
        forbiddenFn?.({ url });
        throw err; // можно оставить: пусть запрос считается неуспешным
      }
    }

    throw err;
  }
}
