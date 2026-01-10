import { useCallback, useMemo } from "react";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import {
  can as canPermission,
  canAll as canAllPermissions,
  canAny as canAnyPermissions,
} from "./can";

export const usePermissions = () => {
  const { user } = useAuth();
  const permissions = useMemo(
    () => user?.effectivePermissions ?? [],
    [user?.effectivePermissions],
  );

  const can = useCallback(
    (permission: string) => canPermission(permissions, permission),
    [permissions],
  );

  const canAny = useCallback(
    (perms: string[]) => canAnyPermissions(permissions, perms),
    [permissions],
  );

  const canAll = useCallback(
    (perms: string[]) => canAllPermissions(permissions, perms),
    [permissions],
  );

  return {
    can,
    canAny,
    canAll,
    permissionsVersion: user?.permissionsVersion ?? null,
    permissions,
  };
};
