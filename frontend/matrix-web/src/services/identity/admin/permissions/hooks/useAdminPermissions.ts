import { useEffect, useMemo, useState } from "react";
import {
  getPermissionsCatalog,
  getRolePermissions,
  getRolesCatalog,
  updateRolePermissions,
} from "@services/identity/api/admin/adminApi";
import type {
  PermissionCatalogItemResponse,
  RoleResponse,
} from "@services/identity/api/admin/adminTypes";

export function useAdminPermissions() {
  const [loading, setLoading] = useState(false);
  const [roleLoading, setRoleLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [roles, setRoles] = useState<RoleResponse[]>([]);
  const [perms, setPerms] = useState<PermissionCatalogItemResponse[]>([]);
  const [activeRoleId, setActiveRoleId] = useState<string | null>(null);

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
      setRoles(rolesResponse);
      setPerms(permsResponse.filter((x) => !x.isDeprecated));
      setActiveRoleId((prev) => prev ?? rolesResponse[0]?.id ?? null);
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
    if (!activeRoleId) return;
    let active = true;
    setRoleLoading(true);
    setError(null);

    getRolePermissions(activeRoleId)
      .then((response) => {
        if (!active) return;
        setRolePermissions(new Set(response.permissionKeys));
        setDirty(false);
      })
      .catch((error) => {
        console.error(error);
        if (!active) return;
        setError("Failed to load role permissions");
      })
      .finally(() => {
        if (!active) return;
        setRoleLoading(false);
      });

    return () => {
      active = false;
    };
  }, [activeRoleId]);

  const activeRole = useMemo(
    () => roles.find((role) => role.id === activeRoleId) ?? null,
    [roles, activeRoleId]
  );

  const grouped = useMemo(() => {
    const map = new Map<string, PermissionCatalogItemResponse[]>();
    for (const permission of perms) {
      const key = `${permission.service} / ${permission.group}`;
      const entries = map.get(key) ?? [];
      entries.push(permission);
      map.set(key, entries);
    }
    return Array.from(map.entries()).sort((a, b) => a[0].localeCompare(b[0]));
  }, [perms]);

  const togglePermission = (key: string) => {
    setRolePermissions((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
    setDirty(true);
  };

  const saveChanges = async () => {
    if (!activeRoleId) return;
    setSaving(true);
    setError(null);
    try {
      await updateRolePermissions(activeRoleId, Array.from(rolePermissions));
      setDirty(false);
    } catch (error) {
      console.error(error);
      setError("Failed to update role permissions");
    } finally {
      setSaving(false);
    }
  };

  return {
    loading,
    roleLoading,
    saving,
    error,
    roles,
    activeRoleId,
    setActiveRoleId,
    activeRole,
    grouped,
    rolePermissions,
    dirty,
    load,
    togglePermission,
    saveChanges,
  };
}
