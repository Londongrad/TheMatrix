// src/api/http.ts

export class HttpError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

// Колбэки, которые подкинет AuthContext
type RefreshTokenFn = () => Promise<string | null>;
type LogoutFn = () => void;

let refreshTokenFn: RefreshTokenFn | null = null;
let logoutFn: LogoutFn | null = null;

// AuthContext один раз вызовет это при старте
export function configureHttpAuth(options: {
  refreshToken: RefreshTokenFn;
  onLogout: LogoutFn;
}) {
  refreshTokenFn = options.refreshToken;
  logoutFn = options.onLogout;
}

// Базовый helper над fetch
export async function request<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  let response: Response;

  try {
    response = await fetch(url, {
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        ...(options.headers ?? {}),
      },
      ...options,
    });
  } catch {
    // Сетевая ошибка / сервер упал
    throw new HttpError(
      0,
      "Не удалось подключиться к серверу. Попробуйте позже."
    );
  }

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;

    try {
      const text = await response.text();
      if (text) {
        message = text;
      }
    } catch {
      // игнорируем, оставляем дефолтное message
    }

    throw new HttpError(response.status, message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

// 🔥 Глобальный клиент с авто-refresh на 401 + /forbidden на 403
export async function apiRequest<T>(
  url: string,
  options: RequestInit = {},
  opts: { enableAuthRefresh?: boolean } = {}
): Promise<T> {
  const { enableAuthRefresh = true } = opts;

  try {
    // первая попытка
    return await request<T>(url, options);
  } catch (err) {
    if (err instanceof HttpError) {
      // 401 → пробуем refresh, если настроен
      if (err.status === 401 && enableAuthRefresh && refreshTokenFn) {
        try {
          const newToken = await refreshTokenFn();

          // refresh не удался → выходим из системы
          if (!newToken) {
            logoutFn?.();
            throw err;
          }

          // повторяем запрос с новым access token
          const headers: HeadersInit = {
            ...(options.headers ?? {}),
            Authorization: `Bearer ${newToken}`,
          };

          return await request<T>(url, {
            ...options,
            headers,
          });
        } catch {
          // refresh упал (401/500/сеть) → принудительный logout
          logoutFn?.();
          throw err;
        }
      }

      // 403 → страница "Нет доступа"
      if (err.status === 403) {
        window.location.href = "/forbidden";
      }
    }

    // всё остальное отдаём наверх (компонент/страница покажет ошибку)
    throw err;
  }
}
