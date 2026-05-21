import type {ReactElement} from "react";
import {render} from "@testing-library/react";
import {vi} from "vitest";

import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import {AuthContext, type AuthContextValue} from "@services/identity/api/self/auth/authContextShared";

type AuthTestOptions = {
    user?: ProfileResponse | null;
    token?: string | null;
    isLoading?: boolean;
    permissions?: string[];
};

export function createProfileResponse(
    permissions: string[] = [],
): ProfileResponse {
    return {
        userId: "user-1",
        email: "neo@example.com",
        pendingEmail: null,
        username: "neo",
        displayName: "Neo",
        avatarUrl: null,
        isEmailConfirmed: true,
        createdAtUtc: "2026-01-01T00:00:00Z",
        emailConfirmedAtUtc: "2026-01-01T00:00:00Z",
        effectivePermissions: permissions,
        permissionsVersion: 1,
    };
}

export function createAuthContextValue({
    user,
    token = "access-token",
    isLoading = false,
    permissions = [],
}: AuthTestOptions = {}): AuthContextValue {
    const resolvedUser = user === undefined ? createProfileResponse(permissions) : user;

    return {
        user: resolvedUser,
        token,
        isLoading,
        login: vi.fn(async () => undefined),
        register: vi.fn(async () => undefined),
        logout: vi.fn(async () => undefined),
        refreshSession: vi.fn(async () => ({
            accessToken: "access-token",
            shouldLogout: false,
        })),
        reloadMe: vi.fn(async () => resolvedUser),
        patchUser: vi.fn(),
    };
}

export function renderWithAuth(
    ui: ReactElement,
    options: AuthTestOptions = {},
) {
    return render(
        <AuthContext.Provider value={createAuthContextValue(options)}>
            {ui}
        </AuthContext.Provider>,
    );
}
