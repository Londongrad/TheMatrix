import { useEffect, useMemo, useState } from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/LoadingIndicator/LoadingIndicator";
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
import "./admin-permissions-page.css";

export default function AdminPermissionsPage() {
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
      const [r, p] = await Promise.all([
        getRolesCatalog(),
        getPermissionsCatalog(),
      ]);
      setRoles(r);
      setPerms(p.filter((x) => !x.isDeprecated));
      setActiveRoleId((prev) => prev ?? r[0]?.id ?? null);
    } catch (e: any) {
      setError(e?.message ?? "Failed to load catalog");
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
      .catch((e) => {
        console.error(e);
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
    () => roles.find((r) => r.id === activeRoleId) ?? null,
    [roles, activeRoleId]
  );

  const grouped = useMemo(() => {
    const map = new Map<string, PermissionCatalogItemResponse[]>();
    for (const p of perms) {
      const key = `${p.service} / ${p.group}`;
      const arr = map.get(key) ?? [];
      arr.push(p);
      map.set(key, arr);
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
    } catch (e) {
      console.error(e);
      setError("Failed to update role permissions");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mx-admin-page">
      <Card
        title="Permissions"
        subtitle="Configure permissions inside roles"
        right={
          <div className="mx-admin-perm__headerRight">
            <Button onClick={() => void load()} disabled={loading}>
              Refresh
            </Button>
            <Button
              variant="primary"
              disabled={!activeRole || !dirty || saving}
              onClick={() => void saveChanges()}
            >
              Save changes
            </Button>
          </div>
        }
      >
        {error ? <div className="mx-admin-perm__error">{error}</div> : null}

        {loading ? (
          <div className="mx-admin-perm__loading">
            <LoadingIndicator label="Loading roles and permissions" />
          </div>
        ) : null}

        <div className="mx-admin-perm__layout">
          <div className="mx-admin-perm__roles">
            <div className="mx-admin-perm__sideTitle">Role</div>
            <div className="mx-admin-perm__roleList">
              {roles.map((r) => (
                <button
                  key={r.id}
                  type="button"
                  className={`mx-admin-perm__roleBtn${
                    r.id === activeRoleId ? " is-active" : ""
                  }`}
                  onClick={() => setActiveRoleId(r.id)}
                >
                  <div className="mx-admin-perm__roleName">{r.name}</div>
                  <div className="mx-admin-perm__roleMeta">
                    {r.isSystem ? "System" : "Custom"}
                  </div>
                </button>
              ))}
            </div>
          </div>

          <div className="mx-admin-perm__matrix">
            <div className="mx-admin-perm__matrixHead">
              <div>
                <div className="mx-admin-perm__matrixTitle">
                  {activeRole ? `Role: ${activeRole.name}` : "Select a role"}
                </div>
                <div className="mx-admin-perm__matrixSub">
                  Toggle permissions for the selected role.
                </div>
              </div>
              {roleLoading ? (
                <LoadingIndicator label="Loading role permissions" />
              ) : null}
            </div>

            <div className="mx-admin-perm__groups">
              {grouped.map(([groupKey, items]) => (
                <div key={groupKey} className="mx-admin-perm__group">
                  <div className="mx-admin-perm__groupTitle">{groupKey}</div>

                  <div className="mx-admin-perm__rows">
                    {items.map((p) => (
                      <label key={p.key} className="mx-admin-perm__row">
                        <div className="mx-admin-perm__permKey">{p.key}</div>
                        <div className="mx-admin-perm__permDesc">
                          {p.description}
                        </div>

                        <div className="mx-admin-perm__toggle">
                          <input
                            type="checkbox"
                            checked={rolePermissions.has(p.key)}
                            disabled={!activeRole || roleLoading || loading}
                            onChange={() => togglePermission(p.key)}
                          />
                          <span />
                        </div>
                      </label>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
