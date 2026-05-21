import {describe, expect, it, vi} from "vitest";

import type {NavItem} from "@shared/navigation/Sidebar/types";
import {filterNavItems} from "./filterNavItems";

describe("filterNavItems", () => {
    it("keeps items without permission requirements", () => {
        const items: NavItem[] = [{to: "/home", label: "Home"}];

        const result = filterNavItems(items, {
            canAny: vi.fn(),
            canAll: vi.fn(),
        });

        expect(result).toEqual(items);
    });

    it("keeps item with default any mode when any required permission is granted", () => {
        const item: NavItem = {
            to: "/people",
            label: "People",
            requiredPermissions: ["people.read"],
        };

        const result = filterNavItems([item], {
            canAny: vi.fn(() => true),
            canAll: vi.fn(() => false),
        });

        expect(result).toEqual([item]);
    });

    it("hides item with default any mode when no required permissions are granted", () => {
        const item: NavItem = {
            to: "/people",
            label: "People",
            requiredPermissions: ["people.read"],
        };

        const result = filterNavItems([item], {
            canAny: vi.fn(() => false),
            canAll: vi.fn(() => true),
        });

        expect(result).toEqual([]);
    });

    it("keeps item with all mode when all required permissions are granted", () => {
        const item: NavItem = {
            to: "/admin",
            label: "Admin",
            requiredPermissions: ["users.read", "users.write"],
            requiredPermissionsMode: "all",
        };

        const result = filterNavItems([item], {
            canAny: vi.fn(() => false),
            canAll: vi.fn(() => true),
        });

        expect(result).toEqual([item]);
    });

    it("hides item with all mode when any required permission is missing", () => {
        const item: NavItem = {
            to: "/admin",
            label: "Admin",
            requiredPermissions: ["users.read", "users.write"],
            requiredPermissionsMode: "all",
        };

        const result = filterNavItems([item], {
            canAny: vi.fn(() => true),
            canAll: vi.fn(() => false),
        });

        expect(result).toEqual([]);
    });

    it("disables item instead of hiding when permissionDisplay is disable", () => {
        const item: NavItem = {
            to: "/admin",
            label: "Admin",
            requiredPermissions: ["admin.read"],
            permissionDisplay: "disable",
        };

        const result = filterNavItems([item], {
            canAny: vi.fn(() => false),
            canAll: vi.fn(() => true),
        });

        expect(result).toHaveLength(1);
        expect(result[0]).toMatchObject({
            disabled: true,
            disabledReason: "Недостаточно прав",
        });
    });

    it("keeps custom disabled reason when disabling item", () => {
        const item: NavItem = {
            to: "/admin",
            label: "Admin",
            requiredPermissions: ["admin.read"],
            permissionDisplay: "disable",
            disabledReason: "Admin only",
        };

        const result = filterNavItems([item], {
            canAny: vi.fn(() => false),
            canAll: vi.fn(() => true),
        });

        expect(result[0]?.disabledReason).toBe("Admin only");
    });

    it("does not mutate original item when disabling it", () => {
        const item: NavItem = {
            to: "/admin",
            label: "Admin",
            requiredPermissions: ["admin.read"],
            permissionDisplay: "disable",
        };

        const result = filterNavItems([item], {
            canAny: vi.fn(() => false),
            canAll: vi.fn(() => true),
        });

        expect(item.disabled).toBeUndefined();
        expect(result[0]).not.toBe(item);
    });
});
