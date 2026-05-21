import {describe, expect, it} from "vitest";

import {can, canAll, canAny} from "./can";

describe("can", () => {
    it("returns true when user has permission", () => {
        expect(can(["users.read"], "users.read")).toBe(true);
    });

    it("returns false when user does not have permission", () => {
        expect(can(["users.read"], "users.write")).toBe(false);
    });
});

describe("canAny", () => {
    it("returns true when user has at least one required permission", () => {
        expect(canAny(["users.read"], ["users.write", "users.read"])).toBe(true);
    });

    it("returns false when user has none of required permissions", () => {
        expect(canAny(["users.read"], ["users.write", "users.delete"])).toBe(false);
    });

    it("returns false for empty required permissions", () => {
        expect(canAny(["users.read"], [])).toBe(false);
    });
});

describe("canAll", () => {
    it("returns true when user has all required permissions", () => {
        expect(canAll(["users.read", "users.write"], ["users.read", "users.write"])).toBe(true);
    });

    it("returns false when at least one required permission is missing", () => {
        expect(canAll(["users.read"], ["users.read", "users.write"])).toBe(false);
    });

    it("canAll_ReturnsTrueForEmptyRequiredPermissionsBecauseThereIsNothingToDeny", () => {
        expect(canAll(["users.read"], [])).toBe(true);
    });
});
