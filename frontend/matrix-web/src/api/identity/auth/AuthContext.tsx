// src/api/auth/AuthContext.tsx
import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useRef,
  useCallback,
} from "react";
import type { LoginRequest } from "./authTypes";
import type { ProfileResponse } from "@api/identity/account/accountTypes";
import { getProfile } from "@api/identity/account/accountApi";
import { loginUser, registerUser, refreshAuth, logoutAuth } from "./authApi";
import { configureHttpAuth } from "@api/http";

interface AuthContextValue {
  user: ProfileResponse | null;
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
  reloadMe: () => Promise<ProfileResponse | null>;
  patchUser: (patch: Partial<ProfileResponse>) => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<ProfileResponse | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const hasTriedRefresh = useRef(false);

  // 🔁 Обновление access-токена по refresh-куке
  const refreshSession = useCallback(async (): Promise<string | null> => {
    try {
      const result = await refreshAuth(); // /api/auth/refresh
      const newAccess = result.accessToken;

      setToken(newAccess);

      const me = await getProfile(newAccess);
      setUser(me);

      return newAccess;
    } catch {
      // refresh умер / куки нет / ошибка сети
      setToken(null);
      setUser(null);
      return null;
    }
  }, []);

  const reloadMe = useCallback(async (): Promise<ProfileResponse | null> => {
    if (!token) {
      return null;
    }

    try {
      const me = await getProfile(token);
      setUser(me);
      return me;
    } catch {
      // do not reset auth state here
      return null;
    }
  }, [token]);

  const patchUser = useCallback((patch: Partial<ProfileResponse>) => {
    setUser((prev) => (prev ? { ...prev, ...patch } : prev));
  }, []);

  const login = async (data: LoginRequest) => {
    const result = await loginUser(data);

    const access = result.accessToken;
    setToken(access);

    const me = await getProfile(access);
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
    await login({
      login: data.email,
      password: data.password,
      rememberMe: true,
    });
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
    reloadMe,
    patchUser,
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
