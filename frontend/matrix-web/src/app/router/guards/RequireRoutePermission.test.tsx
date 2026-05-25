// @vitest-environment jsdom

import {cleanup, render, screen} from "@testing-library/react";
import {afterEach, describe, expect, it} from "vitest";
import {MemoryRouter, Route, Routes} from "react-router";

import type {PermissionMatchMode} from "@shared/permissions/permissionMatchMode";
import {AuthContext} from "@services/identity/api/self/auth/authContextShared";
import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import {createAuthContextValue, createProfileResponse} from "../../../test/authTestUtils";
import {RequireRoutePermission} from "./RequireRoutePermission";

type RenderProtectedRouteOptions = {
    permissions?: string[];
    permissionMatchMode?: PermissionMatchMode;
    userPermissions?: string[];
    user?: ProfileResponse | null;
    isLoading?: boolean;
};

function renderProtectedRoute({
                                  permissions = ["secret.read"],
                                  permissionMatchMode = "any",
                                  userPermissions = [],
                                  user,
                                  isLoading = false,
                              }: RenderProtectedRouteOptions = {}) {
    const resolvedUser = user === undefined
        ? createProfileResponse(userPermissions)
        : user;

    return render(
        <AuthContext.Provider
            value={createAuthContextValue({
                user: resolvedUser,
                isLoading,
            })}
        >
            <MemoryRouter initialEntries={["/secret"]}>
                <Routes>
                    <Route
                        path="/secret"
                        element={
                            <RequireRoutePermission
                                permissions={permissions}
                                permissionMatchMode={permissionMatchMode}
                            >
                                <div>Secret page</div>
                            </RequireRoutePermission>
                        }
                    />
                    <Route path="/login" element={<div>Login page</div>}/>
                    <Route path="/forbidden" element={<div>Forbidden page</div>}/>
                </Routes>
            </MemoryRouter>
        </AuthContext.Provider>,
    );
}

afterEach(() => {
    cleanup();
});

describe("RequireRoutePermission", () => {
    it("renders loading screen while auth state is loading", () => {
        renderProtectedRoute({
            isLoading: true,
        });

        expect(screen.getByText("Loading the workspace...")).not.toBeNull();
        expect(screen.getByText("Restoring your session and permissions.")).not.toBeNull();
    });

    it("redirects unauthenticated users to login", () => {
        renderProtectedRoute({
            user: null,
        });

        expect(screen.getByText("Login page")).not.toBeNull();
    });

    it("redirects authenticated users without permission to forbidden", () => {
        renderProtectedRoute({
            permissions: ["secret.read"],
            userPermissions: ["profile.read"],
        });

        expect(screen.getByText("Forbidden page")).not.toBeNull();
    });

    it("renders children when any required permission is present", () => {
        renderProtectedRoute({
            permissions: ["secret.read", "secret.write"],
            userPermissions: ["secret.write"],
        });

        expect(screen.getByText("Secret page")).not.toBeNull();
    });

    it("redirects to forbidden when all mode is missing a required permission", () => {
        renderProtectedRoute({
            permissions: ["secret.read", "secret.write"],
            permissionMatchMode: "all",
            userPermissions: ["secret.read"],
        });

        expect(screen.getByText("Forbidden page")).not.toBeNull();
    });

    it("renders children when all mode has every required permission", () => {
        renderProtectedRoute({
            permissions: ["secret.read", "secret.write"],
            permissionMatchMode: "all",
            userPermissions: ["secret.read", "secret.write"],
        });

        expect(screen.getByText("Secret page")).not.toBeNull();
    });
});
