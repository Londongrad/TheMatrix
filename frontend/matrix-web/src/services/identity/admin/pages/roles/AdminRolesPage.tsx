import { useEffect, useState } from "react";
import AdminCard from "../../ui/components/AdminCard";
import AdminButton from "../../ui/components/AdminButton";
import { getRolesCatalog } from "@services/identity/api/admin/adminApi";
import type { RoleResponse } from "@services/identity/api/admin/adminTypes";
import "./admin-roles-page.css";

export default function AdminRolesPage() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [roles, setRoles] = useState<RoleResponse[]>([]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await getRolesCatalog();
      setRoles(res);
    } catch (e: any) {
      setError(e?.message ?? "Failed to load roles");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  return (
    <div className="mx-admin-page">
      <AdminCard
        title="Roles"
        subtitle="Access groups"
        right={
          <div className="mx-admin-roles__headerRight">
            <AdminButton onClick={() => void load()} disabled={loading}>
              Refresh
            </AdminButton>
            <AdminButton variant="primary" type="button">
              + New role
            </AdminButton>
          </div>
        }
      >
        {error ? <div className="mx-admin-roles__error">{error}</div> : null}

        <div className="mx-admin-roles__list">
          {roles.map((r) => (
            <div key={r.id} className="mx-admin-roles__item">
              <div className="mx-admin-roles__head">
                <div className="mx-admin-roles__name">{r.name}</div>
                <div className="mx-admin-roles__chips">
                  {r.isSystem ? (
                    <span className="mx-admin-roles__chip">System</span>
                  ) : null}
                  <span className="mx-admin-roles__chip">
                    {/* TODO real count */}— users
                  </span>
                </div>
              </div>

              <div className="mx-admin-roles__meta">
                <span className="mx-admin-roles__mono">{r.id}</span>
                <span className="mx-admin-roles__muted">•</span>
                <span className="mx-admin-roles__muted">
                  {r.createdAtUtc.replace("T", " ").replace("Z", "")}
                </span>
              </div>

              <div className="mx-admin-roles__actions">
                <AdminButton type="button">Members</AdminButton>
                <AdminButton type="button">Permissions</AdminButton>
              </div>
            </div>
          ))}
        </div>
      </AdminCard>
    </div>
  );
}
