// src/services/identity/api/auth/AuthContext.tsx
import {
  createContext,
  useContext,
  useEffect,
  useState,
  useRef,
  useCallback,
  type PropsWithChildren,
} from "react";
import { useNavigate, useLocation } from "react-router-dom";
import type { LoginRequest } from "./authTypes";
import type { ProfileResponse } from "@services/identity/api/account/accountTypes";
import { getProfile } from "@services/identity/api/account/accountApi";
import { loginUser, registerUser, refreshAuth, logoutAuth } from "./authApi";
import { configureHttpAuth } from "@shared/api/http";

interface AuthContextValue {
  user: ProfileResponse | null;
  token: string | null;
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: {
    email: string;
    username: string;
    password: string;
    confirmPassword: string;
  }) => Promise<void>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<string | null>;
  reloadMe: () => Promise<ProfileResponse | null>;
  patchUser: (patch: Partial<ProfileResponse>) => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider = ({ children }: PropsWithChildren) => {
  const [user, setUser] = useState<ProfileResponse | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const tokenRef = useRef<string | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const hasTriedRefresh = useRef(false);

  const navigate = useNavigate();
  const location = useLocation();

  const setAccessToken = useCallback((value: string | null) => {
    tokenRef.current = value;
    setToken(value);
  }, []);

  // 🔁 Обновление access-токена по refresh-куке (ТОЛЬКО токен)
  const refreshSession = useCallback(async (): Promise<string | null> => {
    try {
      const result = await refreshAuth(); // /api/auth/refresh
      const newAccess = result.accessToken;

      setAccessToken(newAccess); // tokenRef + state

      return newAccess;
    } catch {
      setAccessToken(null);
      setUser(null);
      return null;
    }
  }, [setAccessToken]);

  const reloadMe = useCallback(async (): Promise<ProfileResponse | null> => {
    const current = tokenRef.current;
    if (!current) return null;

    try {
      const me = await getProfile();
      setUser(me);
      return me;
    } catch {
      return null;
    }
  }, []);

  const patchUser = useCallback((patch: Partial<ProfileResponse>) => {
    setUser((prev) => (prev ? { ...prev, ...patch } : prev));
  }, []);

  const login = async (data: LoginRequest) => {
    const result = await loginUser(data);
    const access = result.accessToken;

    setAccessToken(access);

    const me = await getProfile();
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
      setAccessToken(null);
      setUser(null);
    }
  };

  // При монтировании: один раз пробуем восстановить сессию по refresh-куке
  useEffect(() => {
    if (hasTriedRefresh.current) return;
    hasTriedRefresh.current = true;

    (async () => {
      const access = await refreshSession();
      if (access) {
        await reloadMe(); // отдельный вызов профиля ОДИН РАЗ на старте
      }
      setIsLoading(false);
    })();
  }, [refreshSession, reloadMe]);

  // 🔥 configure один раз (refreshSession стабилен через useCallback)
  useEffect(() => {
    configureHttpAuth({
      refreshToken: refreshSession,
      onLogout: () => {
        setAccessToken(null);
        setUser(null);
      },
      getAccessToken: () => tokenRef.current,
      onForbidden: ({ url }) => {
        // чтобы не зациклиться, если уже на forbidden
        if (location.pathname !== "/forbidden") {
          navigate("/forbidden", {
            replace: true,
            state: { from: location.pathname, url },
          });
        }
      },
    });
  }, [refreshSession, setAccessToken, navigate, location.pathname]);

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
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
};
