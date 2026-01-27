import {useEffect, useMemo, useState} from "react";
import {
    assignUserRoles,
    depriveUserPermission,
    getPermissionsCatalog,
    getRolePermissions,
    getRolesCatalog,
    getUserDetails,
    getUserPermissions,
    getUserRoles,
    grantUserPermission,
} from "@services/identity/api/admin/adminApi";
import type {
    PermissionCatalogItemResponse,
    RoleResponse,
    UserDetailsResponse,
    UserPermissionResponse,
    UserRoleResponse,
} from "@services/identity/api/admin/adminTypes";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";

const SUPER_ADMIN_ROLE_NAME = "SuperAdmin";

export function useUserAccess(userId: string) {
    const {user: currentUser} = useAuth();

    const [loading, setLoading] = useState(true);
    const [savingRoles, setSavingRoles] = useState(false);
    const [savingPermission, setSavingPermission] = useState<string | null>(null);
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
                setRolesCatalog(roles);
                setUserRoles(assignedRoles);
                setSelectedRoleIds(assignedRoles.map((role) => role.id));
                setPermissionsCatalog(perms.filter((permission) => !permission.isDeprecated));
                setUserPermissions(overrides);

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

    const isCurrentUser = details?.id === currentUser?.userId;
    const isProtectedUser = useMemo(
        () =>
            userRoles.some(
                (role) => role.name === SUPER_ADMIN_ROLE_NAME
            ),
        [userRoles]
    );
    const isAccessReadOnly = isCurrentUser || isProtectedUser;
    const readOnlyReason = isCurrentUser
        ? "You cannot manage your own access from the admin panel."
        : isProtectedUser
            ? "SuperAdmin access is protected and can only be viewed here."
            : null;

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

    const updatePermission = async (
        permissionKey: string,
        effect: "Allow" | "Deny"
    ) => {
        if (isAccessReadOnly) return;

        setSavingPermission(permissionKey);
        setError(null);
        try {
            if (effect === "Allow") {
                await grantUserPermission(userId, permissionKey);
            } else {
                await depriveUserPermission(userId, permissionKey);
            }

            setUserPermissions((prev) => {
                const updated = prev.filter(
                    (permission) => permission.permissionKey !== permissionKey
                );
                updated.push({permissionKey, effect});
                return updated;
            });

            const updated = await getUserPermissions(userId);
            setUserPermissions(updated);
        } catch (error) {
            console.error(error);
            setError("Failed to update permissions");
        } finally {
            setSavingPermission(null);
        }
    };

    return {
        loading,
        savingRoles,
        savingPermission,
        error,
        details,
        rolesCatalog,
        userRoles,
        permissionsCatalog,
        permissionMap,
        rolePermissionKeys,
        selectedRoleIds,
        setSelectedRoleIds,
        saveRoles,
        updatePermission,
        isCurrentUser,
        isProtectedUser,
        isAccessReadOnly,
        readOnlyReason,
    };
}