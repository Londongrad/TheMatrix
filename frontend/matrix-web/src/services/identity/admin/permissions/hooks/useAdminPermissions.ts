import {useEffect, useMemo, useState} from "react";
import {
    getDefaultUserAccessPermissions,
    getPermissionsCatalog,
    getRolePermissions,
    getRolesCatalog,
    updateDefaultUserAccessPermissions,
    updateRolePermissions,
} from "@services/identity/api/admin/adminApi";
import type {PermissionCatalogItemResponse, RoleResponse,} from "@services/identity/api/admin/adminTypes";
import {filterVisibleAdminRoles} from "@services/identity/admin/shared/utils/roleVisibility";

export type PermissionGroup = {
    title: string;
    items: PermissionCatalogItemResponse[];
};

export type PermissionSection = {
    title: string;
    groups: PermissionGroup[];
};

export const DEFAULT_USER_ACCESS_SCOPE_ID = "default-user-access";

export type PermissionScope = {
    id: string;
    name: string;
    kind: "default-user-access" | "role";
    meta: string;
    editable: boolean;
    note: string | null;
    version: number | null;
    role: RoleResponse | null;
};

export function useAdminPermissions() {
    const [loading, setLoading] = useState(false);
    const [roleLoading, setRoleLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [roles, setRoles] = useState<RoleResponse[]>([]);
    const [perms, setPerms] = useState<PermissionCatalogItemResponse[]>([]);
    const [activeScopeId, setActiveScopeId] = useState<string | null>(DEFAULT_USER_ACCESS_SCOPE_ID);
    const [defaultUserAccessVersion, setDefaultUserAccessVersion] = useState<number | null>(null);

    const [rolePermissions, setRolePermissions] = useState<Set<string>>(
        new Set()
    );
    const [dirty, setDirty] = useState(false);

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const [rolesResponse, permsResponse] = await Promise.all([
                getRolesCatalog(),
                getPermissionsCatalog(),
            ]);
            const visibleRoles = filterVisibleAdminRoles(rolesResponse);
            setRoles(visibleRoles);
            setPerms(permsResponse.filter((x) => !x.isDeprecated));
            setActiveScopeId((prev) =>
                prev === DEFAULT_USER_ACCESS_SCOPE_ID ||
                (prev && visibleRoles.some((role) => role.id === prev))
                    ? prev
                    : DEFAULT_USER_ACCESS_SCOPE_ID
            );
        } catch (error: any) {
            setError(error?.message ?? "Failed to load catalog");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void load();
    }, []);

    useEffect(() => {
        if (!activeScopeId) return;
        let active = true;
        setRoleLoading(true);
        setError(null);

        const loadPermissions: Promise<void> =
            activeScopeId === DEFAULT_USER_ACCESS_SCOPE_ID
                ? getDefaultUserAccessPermissions().then((response) => {
                    if (!active) return;
                    setDefaultUserAccessVersion(response.version);
                    setRolePermissions(new Set(response.permissionKeys));
                    setDirty(false);
                })
                : getRolePermissions(activeScopeId).then((response) => {
                    if (!active) return;
                    setDefaultUserAccessVersion(null);
                    setRolePermissions(new Set(response.permissionKeys));
                    setDirty(false);
                });

        loadPermissions
            .catch((error) => {
                console.error(error);
                if (!active) return;
                setError(
                    activeScopeId === DEFAULT_USER_ACCESS_SCOPE_ID
                        ? "Failed to load default user access"
                        : "Failed to load role permissions"
                );
            })
            .finally(() => {
                if (!active) return;
                setRoleLoading(false);
            });

        return () => {
            active = false;
        };
    }, [activeScopeId]);

    const scopes = useMemo<PermissionScope[]>(() => {
        const base: PermissionScope[] = [
            {
                id: DEFAULT_USER_ACCESS_SCOPE_ID,
                name: "Default user access",
                kind: "default-user-access",
                meta: "Mutable baseline for all User accounts",
                editable: true,
                note: "Use this to grant or deny the default access inherited by ordinary users without editing the immutable system role User.",
                version: defaultUserAccessVersion,
                role: null,
            },
        ];

        return base.concat(
            roles.map((role) => ({
                id: role.id,
                name: role.name,
                kind: "role" as const,
                meta: role.isSystem ? "System role" : "Custom role",
                editable: !role.isSystem,
                note: role.isSystem
                    ? role.name === "User"
                        ? "System role User is read-only. Change ordinary-user defaults through Default user access."
                        : `System role ${role.name} is read-only.`
                    : null,
                version: null,
                role,
            }))
        );
    }, [roles, defaultUserAccessVersion]);

    const activeScope = useMemo(
        () => scopes.find((scope) => scope.id === activeScopeId) ?? null,
        [scopes, activeScopeId]
    );

    const grouped = useMemo<PermissionSection[]>(() => {
        const sectionMap = new Map<
            string,
            Map<string, PermissionCatalogItemResponse[]>
        >();
        for (const permission of perms) {
            const [category, ...rest] = permission.group.split(" / ");
            const subgroup = rest.join(" / ");
            const sectionTitle = `${permission.service} / ${category}`;
            const subgroupTitle = subgroup || "General";

            const subMap = sectionMap.get(sectionTitle) ?? new Map();
            const entries = subMap.get(subgroupTitle) ?? [];
            entries.push(permission);
            subMap.set(subgroupTitle, entries);
            sectionMap.set(sectionTitle, subMap);
        }
        return Array.from(sectionMap.entries())
            .map(([title, groups]) => ({
                title,
                groups: Array.from(groups.entries())
                    .map(([groupTitle, items]) => ({
                        title: groupTitle,
                        items,
                    }))
                    .sort((a, b) => a.title.localeCompare(b.title)),
            }))
            .sort((a, b) => a.title.localeCompare(b.title));
    }, [perms]);

    const togglePermission = (key: string) => {
        if (!activeScope?.editable) {
            return;
        }

        setRolePermissions((prev) => {
            const next = new Set(prev);
            if (next.has(key)) next.delete(key);
            else next.add(key);
            return next;
        });
        setDirty(true);
    };

    const saveChanges = async () => {
        if (!activeScope?.editable) return;
        setSaving(true);
        setError(null);
        try {
            const permissionKeys = Array.from(rolePermissions);
            if (activeScope.kind === "default-user-access") {
                await updateDefaultUserAccessPermissions({permissionKeys});
                const response = await getDefaultUserAccessPermissions();
                setDefaultUserAccessVersion(response.version);
                setRolePermissions(new Set(response.permissionKeys));
            } else {
                await updateRolePermissions(activeScope.id, permissionKeys);
            }
            setDirty(false);
        } catch (error) {
            console.error(error);
            setError(
                activeScope.kind === "default-user-access"
                    ? "Failed to update default user access"
                    : "Failed to update role permissions"
            );
        } finally {
            setSaving(false);
        }
    };

    return {
        loading,
        roleLoading,
        saving,
        error,
        scopes,
        activeScopeId,
        setActiveScopeId,
        activeScope,
        grouped,
        rolePermissions,
        dirty,
        load,
        togglePermission,
        saveChanges,
    };
}
