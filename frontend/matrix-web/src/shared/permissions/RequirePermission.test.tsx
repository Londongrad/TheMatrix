// @vitest-environment jsdom

import {cleanup, screen} from "@testing-library/react";
import {afterEach, describe, expect, it} from "vitest";

import {renderWithAuth} from "../../test/authTestUtils";
import {RequirePermission, RequirePermissions} from "./RequirePermission";

afterEach(() => {
    cleanup();
});

describe("RequirePermission", () => {
    it("renders children when permission is granted", () => {
        renderWithAuth(
            <RequirePermission perm="identity.admin.users.read">
                <button type="button">Read users</button>
            </RequirePermission>,
            {permissions: ["identity.admin.users.read"]},
        );

        expect(screen.getByText("Read users")).not.toBeNull();
    });

    it("hides children by default when permission is missing", () => {
        renderWithAuth(
            <RequirePermission perm="identity.admin.users.read">
                <button type="button">Read users</button>
            </RequirePermission>,
            {permissions: ["identity.admin.users.write"]},
        );

        expect(screen.queryByText("Read users")).toBeNull();
    });

    it("disables children when displayMode is disable", () => {
        renderWithAuth(
            <RequirePermission
                perm="identity.admin.users.read"
                displayMode="disable"
                tooltip="No access"
            >
                <button type="button">Read users</button>
            </RequirePermission>,
            {permissions: ["identity.admin.users.write"]},
        );

        const wrapper = screen.getByTitle("No access");

        expect(screen.getByText("Read users")).not.toBeNull();
        expect(wrapper.className).toContain("mx-permission");
        expect(wrapper.className).toContain("is-disabled");
    });
});

describe("RequirePermissions", () => {
    it("renders children when any permission is granted by default", () => {
        renderWithAuth(
            <RequirePermissions perms={["users.read", "users.write"]}>
                <button type="button">Manage users</button>
            </RequirePermissions>,
            {permissions: ["users.read"]},
        );

        expect(screen.getByText("Manage users")).not.toBeNull();
    });

    it("hides children when all mode is missing a required permission", () => {
        renderWithAuth(
            <RequirePermissions
                perms={["users.read", "users.write"]}
                permissionMatchMode="all"
            >
                <button type="button">Manage users</button>
            </RequirePermissions>,
            {permissions: ["users.read"]},
        );

        expect(screen.queryByText("Manage users")).toBeNull();
    });

    it("renders children when all mode has every required permission", () => {
        renderWithAuth(
            <RequirePermissions
                perms={["users.read", "users.write"]}
                permissionMatchMode="all"
            >
                <button type="button">Manage users</button>
            </RequirePermissions>,
            {permissions: ["users.read", "users.write"]},
        );

        expect(screen.getByText("Manage users")).not.toBeNull();
    });
});
