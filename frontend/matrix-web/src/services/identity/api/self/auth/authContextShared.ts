import {createContext} from "react";
import type {LoginRequest} from "./authTypes";
import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import type {AuthRefreshResult} from "@shared/api/http";

export interface AuthContextValue {
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
    refreshSession: () => Promise<AuthRefreshResult>;
    reloadMe: () => Promise<ProfileResponse | null>;
    patchUser: (patch: Partial<ProfileResponse>) => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
