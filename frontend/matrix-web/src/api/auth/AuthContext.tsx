import React, { createContext, useContext, useEffect, useState } from "react";
import type { MeResponse, LoginRequest } from "./authApi";
import {
  getMe,
  loginUser,
  registerUser,
  refreshAuth,
  logoutAuth,
} from "./authApi";

interface AuthContextValue {
  user: MeResponse | null;
  token: string | null; // access token
  refreshToken: string | null; // refresh token
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: {
    email: string;
    username: string;
    password: string;
    confirmPassword: string;
  }) => Promise<void>;
  logout: () => void;
  refreshSession: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const ACCESS_TOKEN_KEY = "matrix_access_token";
const REFRESH_TOKEN_KEY = "matrix_refresh_token";

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [user, setUser] = useState<MeResponse | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [refreshToken, setRefreshToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // При монтировании: читаем токены из localStorage и дергаем /me
  useEffect(() => {
    const storedAccess = localStorage.getItem(ACCESS_TOKEN_KEY);
    const storedRefresh = localStorage.getItem(REFRESH_TOKEN_KEY);

    if (!storedAccess || !storedRefresh) {
      setIsLoading(false);
      return;
    }

    (async () => {
      try {
        const me = await getMe(storedAccess);
        setToken(storedAccess);
        setRefreshToken(storedRefresh);
        setUser(me);
      } catch {
        // токены протухли / невалидны
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        setToken(null);
        setRefreshToken(null);
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    })();
  }, []);

  const login = async (data: LoginRequest) => {
    const result = await loginUser(data);

    const access = result.accessToken;
    const refresh = result.refreshToken;

    localStorage.setItem(ACCESS_TOKEN_KEY, access);
    localStorage.setItem(REFRESH_TOKEN_KEY, refresh);

    setToken(access);
    setRefreshToken(refresh);

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

  // Обновление access/refresh по refresh-токену
  const refreshSession = async () => {
    if (!refreshToken) {
      throw new Error("No refresh token");
    }

    const result = await refreshAuth(refreshToken);

    const newAccess = result.accessToken;
    const newRefresh = result.refreshToken;

    localStorage.setItem(ACCESS_TOKEN_KEY, newAccess);
    localStorage.setItem(REFRESH_TOKEN_KEY, newRefresh);

    setToken(newAccess);
    setRefreshToken(newRefresh);

    const me = await getMe(newAccess);
    setUser(me);
  };

  const logout = () => {
    if (refreshToken) {
      // отзываем refresh-токен на бэке, но не ждем результат
      logoutAuth(refreshToken).catch(() => {});
    }

    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);

    setToken(null);
    setRefreshToken(null);
    setUser(null);
  };

  const value: AuthContextValue = {
    user,
    token,
    refreshToken,
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
