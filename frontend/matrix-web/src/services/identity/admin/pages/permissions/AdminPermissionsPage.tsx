import { useEffect, useMemo, useState } from "react";
import AdminCard from "../../ui/components/AdminCard";
import AdminButton from "../../ui/components/AdminButton";
import {
  getPermissionsCatalog,
  getRolesCatalog,
} from "@services/identity/api/admin/adminApi";
import type {
  PermissionCatalogItemResponse,
  RoleResponse,
} from "@services/identity/api/admin/adminTypes";
import "./admin-permissions-page.css";

export default function AdminPermissionsPage() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [roles, setRoles] = useState<RoleResponse[]>([]);
  const [perms, setPerms] = useState<PermissionCatalogItemResponse[]>([]);

  const [activeRoleId, setActiveRoleId] = useState<string | null>(null);

  // TODO: сюда подтягиваешь текущие пермишены роли:
  // const [rolePerms, setRolePerms] = useState<Set<string>>(new Set());

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

  return (
    <div className="mx-admin-page">
      <AdminCard
        title="Permissions"
        subtitle="Configure permissions inside roles"
        right={
          <div className="mx-admin-perm__headerRight">
            <AdminButton onClick={() => void load()} disabled={loading}>
              Refresh
            </AdminButton>
            <AdminButton variant="primary" disabled={!activeRole}>
              Save changes
            </AdminButton>
          </div>
        }
      >
        {error ? <div className="mx-admin-perm__error">{error}</div> : null}

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
                  Toggle permissions for the selected role (UI ready, connect
                  API).
                </div>
              </div>
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
                            // TODO: checked={rolePerms.has(p.key)}
                            defaultChecked={Math.random() > 0.6}
                            disabled={!activeRole}
                            onChange={() => {
                              // TODO: обновляешь локальное состояние, потом Save отправляет на бек
                            }}
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
      </AdminCard>
    </div>
  );
}
