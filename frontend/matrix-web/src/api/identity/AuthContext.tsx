// src/api/auth/AuthContext.tsx
import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useRef,
  useCallback,
} from "react";
import type { MeResponse, LoginRequest } from "./authTypes";
import {
  getMe,
  loginUser,
  registerUser,
  refreshAuth,
  logoutAuth,
} from "./authApi";
import { configureHttpAuth } from "../http";

interface AuthContextValue {
  user: MeResponse | null;
  token: string | null; // access token (in memory only)
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: {
    email: string;
    username: string;
    password: string;
    confirmPassword: string;
  }) => Promise<void>;
  logout: () => Promise<void>;
  // теперь возвращает новый access token или null
  refreshSession: () => Promise<string | null>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<MeResponse | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const hasTriedRefresh = useRef(false);

  // 🔁 Обновление access-токена по refresh-куке
  const refreshSession = useCallback(async (): Promise<string | null> => {
    try {
      const result = await refreshAuth(); // /api/auth/refresh
      const newAccess = result.accessToken;

      setToken(newAccess);

      const me = await getMe(newAccess);
      setUser(me);

      return newAccess;
    } catch {
      // refresh умер / куки нет / ошибка сети
      setToken(null);
      setUser(null);
      return null;
    }
  }, []);

  const login = async (data: LoginRequest) => {
    const result = await loginUser(data);

    const access = result.accessToken;
    setToken(access);

    const me = await getMe(access);
    setUser(me);
  };

  const register = async (data: {
    email: string;
    username: string;
    password: string;
    confirmPassword: string;
  }) => {
    await registerUser({
      email: data.email,
      username: data.username,
      password: data.password,
      confirmPassword: data.confirmPassword,
    });

    // После регистрации сразу логинимся
    await login({ login: data.email, password: data.password });
  };

  const logout = async () => {
    try {
      await logoutAuth(); // бэк сам удалит refresh-куку
    } catch {
      // даже если ошибка, всё равно чистим локальное состояние
    } finally {
      setToken(null);
      setUser(null);
    }
  };

  // При монтировании: один раз пробуем восстановить сессию по refresh-куке
  useEffect(() => {
    if (hasTriedRefresh.current) {
      return;
    }
    hasTriedRefresh.current = true;

    (async () => {
      await refreshSession(); // он сам выставит user/token или обнулит
      setIsLoading(false);
    })();
  }, [refreshSession]);

  // 👉 Подключаем AuthContext к http-слою (для apiRequest)
  useEffect(() => {
    configureHttpAuth({
      refreshToken: refreshSession,
      onLogout: () => {
        setToken(null);
        setUser(null);
      },
    });
  }, [refreshSession]);

  const value: AuthContextValue = {
    user,
    token,
    isLoading,
    login,
    register,
    logout,
    refreshSession,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextValue => {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
};
