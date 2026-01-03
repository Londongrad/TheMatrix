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

export type PermissionGroup = {
  title: string;
  items: PermissionCatalogItemResponse[];
};

export type PermissionSection = {
  title: string;
  groups: PermissionGroup[];
};

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
