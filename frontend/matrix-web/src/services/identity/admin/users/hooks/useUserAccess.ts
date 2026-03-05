import {useEffect, useMemo, useState} from "react";
import {
    assignUserRoles,
    getPermissionsCatalog,
    getRolePermissions,
    getRolesCatalog,
    getUserDetails,
    getUserPermissions,
    getUserRoles,
    updateUserPermissions,
} from "@services/identity/api/admin/adminApi";
import type {
    PermissionCatalogItemResponse,
    PermissionEffect,
    RoleResponse,
    UserDetailsResponse,
    UserPermissionResponse,
    UserRoleResponse,
} from "@services/identity/api/admin/adminTypes";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import {canAll as canAllPermissions} from "@shared/permissions/can";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {
    filterVisibleAdminRoles,
    isHiddenAdminRole,
} from "@services/identity/admin/shared/utils/roleVisibility";

type PermissionDraftMap = Partial<Record<string, PermissionEffect>>;
type PermissionGroup = {
    title: string;
    items: PermissionCatalogItemResponse[];
};

type PermissionSection = {
    title: string;
    groups: PermissionGroup[];
};

function toPermissionDraftMap(
    overrides: UserPermissionResponse[]
): PermissionDraftMap {
    return overrides.reduce<PermissionDraftMap>((draft, override) => {
        draft[override.permissionKey] = override.effect;
        return draft;
    }, {});
}

function arePermissionDraftMapsEqual(
    left: PermissionDraftMap,
    right: PermissionDraftMap
): boolean {
    const leftKeys = Object.keys(left);
    const rightKeys = Object.keys(right);

    if (leftKeys.length !== rightKeys.length) {
        return false;
    }

    return leftKeys.every((key) => left[key] === right[key]);
}

function countPermissionDraftChanges(
    left: PermissionDraftMap,
    right: PermissionDraftMap
): number {
    const keys = new Set([...Object.keys(left), ...Object.keys(right)]);
    let changedCount = 0;

    keys.forEach((key) => {
        if ((left[key] ?? null) !== (right[key] ?? null)) {
            changedCount += 1;
        }
    });

    return changedCount;
}

function groupPermissionsCatalog(
    permissions: PermissionCatalogItemResponse[]
): PermissionSection[] {
    const sectionMap = new Map<string, Map<string, PermissionCatalogItemResponse[]>>();

    for (const permission of permissions) {
        const [category, ...rest] = permission.group.split(" / ");
        const subgroup = rest.join(" / ");
        const sectionTitle = `${permission.service} / ${category}`;
        const subgroupTitle = subgroup || "General";

        const groups = sectionMap.get(sectionTitle) ?? new Map();
        const items = groups.get(subgroupTitle) ?? [];
        items.push(permission);
        groups.set(subgroupTitle, items);
        sectionMap.set(sectionTitle, groups);
    }

    return Array.from(sectionMap.entries())
        .map(([title, groups]) => ({
            title,
            groups: Array.from(groups.entries())
                .map(([groupTitle, items]) => ({
                    title: groupTitle,
                    items,
                }))
                .sort((left, right) => left.title.localeCompare(right.title)),
        }))
        .sort((left, right) => left.title.localeCompare(right.title));
}

export function useUserAccess(userId: string) {
    const {user: currentUser} = useAuth();

    const [loading, setLoading] = useState(true);
    const [savingRoles, setSavingRoles] = useState(false);
    const [savingPermissions, setSavingPermissions] = useState(false);
    const [isEditingPermissions, setIsEditingPermissions] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [details, setDetails] = useState<UserDetailsResponse | null>(null);
    const [rolesCatalog, setRolesCatalog] = useState<RoleResponse[]>([]);
    const [userRoles, setUserRoles] = useState<UserRoleResponse[]>([]);
    const [permissionsCatalog, setPermissionsCatalog] = useState<
        PermissionCatalogItemResponse[]
    >([]);
    const [userPermissions, setUserPermissions] = useState<
        UserPermissionResponse[]
    >([]);
    const [rolePermissionKeys, setRolePermissionKeys] = useState<Set<string>>(
        new Set()
    );

    const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);
    const [permissionDraftMap, setPermissionDraftMap] = useState<PermissionDraftMap>(
        {}
    );

    useEffect(() => {
        let active = true;
        setLoading(true);
        setError(null);

        const load = async () => {
            try {
                const [user, roles, assignedRoles, perms, overrides] =
                    await Promise.all([
                        getUserDetails(userId),
                        getRolesCatalog(),
                        getUserRoles(userId),
                        getPermissionsCatalog(),
                        getUserPermissions(userId),
                    ]);
                if (!active) return;
                setDetails(user);
                setRolesCatalog(filterVisibleAdminRoles(roles));
                setUserRoles(assignedRoles);
                setSelectedRoleIds(assignedRoles.map((role) => role.id));
                setPermissionsCatalog(perms.filter((permission) => !permission.isDeprecated));
                setUserPermissions(overrides);
                setPermissionDraftMap(toPermissionDraftMap(overrides));
                setIsEditingPermissions(false);

                const rolePermissions = await Promise.all(
                    assignedRoles.map((role) => getRolePermissions(role.id))
                );
                if (!active) return;
                setRolePermissionKeys(
                    new Set(
                        rolePermissions.flatMap((permission) => permission.permissionKeys)
                    )
                );
            } catch (error) {
                console.error(error);
                if (!active) return;
                setError("Failed to load user access data");
            } finally {
                if (!active) return;
                setLoading(false);
            }
        };

        void load();

        return () => {
            active = false;
        };
    }, [userId]);

    const permissionMap = useMemo(() => {
        const map = new Map<string, UserPermissionResponse>();
        userPermissions.forEach((permission) =>
            map.set(permission.permissionKey, permission)
        );
        return map;
    }, [userPermissions]);
    const groupedPermissions = useMemo(
        () => groupPermissionsCatalog(permissionsCatalog),
        [permissionsCatalog]
    );
    const savedPermissionDraftMap = useMemo(
        () => toPermissionDraftMap(userPermissions),
        [userPermissions]
    );

    const isCurrentUser = details?.id === currentUser?.userId;
    const isDeletedUser = details?.isDeleted ?? false;
    const isProtectedUser = useMemo(
        () =>
            userRoles.some(
                (role) => isHiddenAdminRole(role)
            ),
        [userRoles]
    );
    const isAccessReadOnly = isCurrentUser || isProtectedUser || isDeletedUser;
    const canEditPermissionOverrides = useMemo(
        () =>
            canAllPermissions(currentUser?.effectivePermissions ?? [], [
                PermissionKeys.IdentityUserPermissionsGrant,
                PermissionKeys.IdentityUserPermissionsDeprive,
            ]),
        [currentUser?.effectivePermissions]
    );
    const readOnlyReason = isCurrentUser
        ? "You cannot manage your own access from the admin panel."
        : isDeletedUser
            ? "Restore the account before editing roles or direct permission overrides."
        : isProtectedUser
            ? "This account has protected system access that is not editable here."
            : null;
    const permissionEditReason = isAccessReadOnly
        ? readOnlyReason
        : !canEditPermissionOverrides
            ? "You need both grant and deprive permissions to edit direct overrides in bulk."
            : null;
    const hasPermissionChanges = useMemo(
        () =>
            !arePermissionDraftMapsEqual(permissionDraftMap, savedPermissionDraftMap),
        [permissionDraftMap, savedPermissionDraftMap]
    );
    const pendingPermissionChangesCount = useMemo(
        () =>
            countPermissionDraftChanges(permissionDraftMap, savedPermissionDraftMap),
        [permissionDraftMap, savedPermissionDraftMap]
    );

    const saveRoles = async () => {
        if (isAccessReadOnly) return;

        setSavingRoles(true);
        setError(null);
        try {
            await assignUserRoles(userId, selectedRoleIds);
            const updated = await getUserRoles(userId);
            setUserRoles(updated);
            setSelectedRoleIds(updated.map((role) => role.id));

            const rolePermissions = await Promise.all(
                updated.map((role) => getRolePermissions(role.id))
            );
            setRolePermissionKeys(
                new Set(
                    rolePermissions.flatMap((permission) => permission.permissionKeys)
                )
            );
        } catch (error) {
            console.error(error);
            setError("Failed to update roles");
        } finally {
            setSavingRoles(false);
        }
    };

    const beginPermissionEditing = () => {
        if (isAccessReadOnly || !canEditPermissionOverrides) {
            return;
        }

        setPermissionDraftMap({...savedPermissionDraftMap});
        setIsEditingPermissions(true);
        setError(null);
    };

    const cancelPermissionEditing = () => {
        setPermissionDraftMap({...savedPermissionDraftMap});
        setIsEditingPermissions(false);
        setError(null);
    };

    const setPermissionOverride = (
        permissionKey: string,
        effect: PermissionEffect | "Inherit"
    ) => {
        if (!isEditingPermissions || isAccessReadOnly || !canEditPermissionOverrides) {
            return;
        }

        setPermissionDraftMap((prev) => {
            const next = {...prev};

            if (effect === "Inherit") {
                delete next[permissionKey];
            } else {
                next[permissionKey] = effect;
            }

            return next;
        });
    };

    const savePermissions = async () => {
        if (isAccessReadOnly || !canEditPermissionOverrides) return;

        if (!hasPermissionChanges) {
            setIsEditingPermissions(false);
            return;
        }

        setSavingPermissions(true);
        setError(null);
        try {
            const overrides = Object.entries(permissionDraftMap)
                .filter((entry): entry is [string, PermissionEffect] => entry[1] !== undefined)
                .sort(([leftKey], [rightKey]) => leftKey.localeCompare(rightKey))
                .map(([permissionKey, effect]) => ({
                    permissionKey,
                    effect,
                }));

            await updateUserPermissions(userId, {overrides});

            const updated = await getUserPermissions(userId);
            setUserPermissions(updated);
            setPermissionDraftMap(toPermissionDraftMap(updated));
            setIsEditingPermissions(false);
        } catch (error) {
            console.error(error);
            setError("Failed to update permissions");
        } finally {
            setSavingPermissions(false);
        }
    };

    return {
        loading,
        savingRoles,
        savingPermissions,
        isEditingPermissions,
        error,
        details,
        rolesCatalog,
        userRoles,
        permissionsCatalog,
        groupedPermissions,
        permissionMap,
        permissionDraftMap,
        rolePermissionKeys,
        selectedRoleIds,
        setSelectedRoleIds,
        saveRoles,
        beginPermissionEditing,
        cancelPermissionEditing,
        setPermissionOverride,
        savePermissions,
        hasPermissionChanges,
        pendingPermissionChangesCount,
        canEditPermissionOverrides,
        permissionEditReason,
        isDeletedUser,
        isCurrentUser,
        isProtectedUser,
        isAccessReadOnly,
        readOnlyReason,
    };
}
