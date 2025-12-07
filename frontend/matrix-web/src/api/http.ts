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

    try {
      const contentType = response.headers.get("Content-Type") || "";
      const text = await response.text();

      if (text) {
        // если пришёл JSON (ProblemDetails)
        if (contentType.includes("application/json")) {
          try {
            const data = JSON.parse(text);

            if (typeof data === "string") {
              message = data;
            } else if (data.detail) {
              // ASP.NET Core ProblemDetails.Detail
              message = data.detail;
            } else if (data.title) {
              // ASP.NET Core ProblemDetails.Title
              message = data.title;
            } else {
              message = text;
            }
          } catch {
            // не смогли распарсить json → оставляем сырой текст
            message = text;
          }
        } else {
          // не json → просто показываем текст
          message = text;
        }
      }
    } catch {
      // игнорируем, оставляем дефолтное message
    }

    // Доп. обработка для 415, если вдруг бек ничего умного не дал
    if (status === 415 && message === `Request failed with status ${status}`) {
      message =
        "Сервер не принимает такой формат файла. Попробуйте загрузить PNG или JPG размером до 2 МБ.";
    }

    throw new HttpError(status, message);
  }

  if (response.status === 204) {
    // No Content
    return undefined as T;
  }

  // предполагаем JSON-ответ
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
