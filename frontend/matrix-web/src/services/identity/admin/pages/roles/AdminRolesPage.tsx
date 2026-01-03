import { useEffect, useMemo, useState } from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import Pagination from "@shared/ui/Pagination/Pagination";
import LoadingIndicator from "@shared/ui/LoadingIndicator/LoadingIndicator";
import Modal from "@shared/ui/Modal/Modal";
import { usePagedQuery } from "@shared/lib/paging/usePagedQuery";
import { useConfirm } from "@shared/ui/ConfirmDialog/ConfirmDialog";
import {
  createRole,
  deleteRole,
  getPermissionsCatalog,
  getRoleMembersPage,
  getRolePermissions,
  getRolesCatalog,
  renameRole,
} from "@services/identity/api/admin/adminApi";
import type {
  PermissionCatalogItemResponse,
  RolePermissionsResponse,
  RenameRoleRequest,
  RoleResponse,
  UserListItemResponse,
} from "@services/identity/api/admin/adminTypes";
import "./admin-roles-page.css";

export default function AdminRolesPage() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [roles, setRoles] = useState<RoleResponse[]>([]);
  const [createOpen, setCreateOpen] = useState(false);
  const [renameRoleTarget, setRenameRoleTarget] = useState<RoleResponse | null>(
    null
  );
  const [membersRole, setMembersRole] = useState<RoleResponse | null>(null);
  const [permissionsRole, setPermissionsRole] = useState<RoleResponse | null>(
    null
  );
  const confirm = useConfirm();

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

  const handleDelete = async (role: RoleResponse) => {
    const accepted = await confirm({
      title: `Delete role "${role.name}"?`,
      description: "All users assigned to this role will lose it.",
      confirmText: "Delete",
      cancelText: "Cancel",
      tone: "danger",
    });

    if (!accepted) return;

    setLoading(true);
    setError(null);
    try {
      await deleteRole(role.id);
      await load();
    } catch (e: any) {
      setError(e?.message ?? "Failed to delete role");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-admin-page">
      <Card
        title="Roles"
        subtitle="Access groups"
        right={
          <div className="mx-admin-roles__headerRight">
            <Button onClick={() => void load()} disabled={loading}>
              Refresh
            </Button>
            <Button
              variant="primary"
              type="button"
              onClick={() => setCreateOpen(true)}
            >
              + New role
            </Button>
          </div>
        }
      >
        {error ? <div className="mx-admin-roles__error">{error}</div> : null}

        {loading && roles.length === 0 ? (
          <div className="mx-admin-roles__loading">
            <LoadingIndicator label="Loading roles" />
          </div>
        ) : null}

        <div className="mx-admin-roles__list">
          {roles.map((r) => (
            <div key={r.id} className="mx-admin-roles__item">
              <div className="mx-admin-roles__head">
                <div className="mx-admin-roles__name">{r.name}</div>
                <div className="mx-admin-roles__chips">
                  {r.isSystem ? (
                    <span className="mx-admin-roles__chip">System</span>
                  ) : null}
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
                <Button type="button" onClick={() => setMembersRole(r)}>
                  Members
                </Button>
                <Button type="button" onClick={() => setPermissionsRole(r)}>
                  Permissions
                </Button>
                <Button
                  type="button"
                  onClick={() => setRenameRoleTarget(r)}
                  disabled={r.isSystem}
                >
                  Rename
                </Button>
                <Button
                  type="button"
                  variant="danger"
                  onClick={() => void handleDelete(r)}
                  disabled={r.isSystem}
                >
                  Delete
                </Button>
              </div>
            </div>
          ))}
        </div>
      </Card>
      {createOpen ? (
        <CreateRoleModal
          onClose={() => setCreateOpen(false)}
          onCreated={load}
        />
      ) : null}

      {renameRoleTarget ? (
        <RenameRoleModal
          role={renameRoleTarget}
          onClose={() => setRenameRoleTarget(null)}
          onUpdated={load}
        />
      ) : null}

      {membersRole ? (
        <RoleMembersModal
          role={membersRole}
          onClose={() => setMembersRole(null)}
        />
      ) : null}

      {permissionsRole ? (
        <RolePermissionsModal
          role={permissionsRole}
          onClose={() => setPermissionsRole(null)}
        />
      ) : null}
    </div>
  );
}

function RenameRoleModal({
  role,
  onClose,
  onUpdated,
}: {
  role: RoleResponse;
  onClose: () => void;
  onUpdated: () => void;
}) {
  const [name, setName] = useState(role.name);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async () => {
    if (!name.trim()) {
      setError("Role name is required");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const payload: RenameRoleRequest = { name: name.trim() };
      await renameRole(role.id, payload);
      await onUpdated();
      onClose();
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? "Failed to rename role");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open
      title={`Rename role · ${role.name}`}
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} disabled={saving}>
            Save
          </Button>
        </>
      }
    >
      {error ? <div className="mx-admin-roles__error">{error}</div> : null}
      <label className="mx-admin-roles__field">
        <span>Role name</span>
        <input
          className="mx-admin-roles__input"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </label>
    </Modal>
  );
}

function CreateRoleModal({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: () => void;
}) {
  const [name, setName] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async () => {
    if (!name.trim()) {
      setError("Role name is required");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await createRole({ name: name.trim() });
      await onCreated();
      onClose();
    } catch (e: any) {
      console.error(e);
      setError(e?.message ?? "Failed to create role");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open
      title="Create role"
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} disabled={saving}>
            Create
          </Button>
        </>
      }
    >
      {error ? <div className="mx-admin-roles__error">{error}</div> : null}
      <label className="mx-admin-roles__field">
        <span>Role name</span>
        <input
          className="mx-admin-roles__input"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </label>
    </Modal>
  );
}

function RoleMembersModal({
  role,
  onClose,
}: {
  role: RoleResponse;
  onClose: () => void;
}) {
  const pageSize = 8;
  const { data, pageNumber, setPageNumber, isLoading, error } = usePagedQuery(
    (page, size) => getRoleMembersPage(role.id, page, size),
    pageSize,
    [role.id],
    {
      errorMessage: "Failed to load members",
    }
  );

  const members = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  return (
    <Modal
      open
      title={`Members · ${role.name}`}
      onClose={onClose}
      footer={
        <Button variant="primary" onClick={onClose}>
          Close
        </Button>
      }
    >
      {error ? <div className="mx-admin-roles__error">{error}</div> : null}
      {isLoading && members.length === 0 ? (
        <div className="mx-admin-roles__loading">
          <LoadingIndicator label="Loading members" />
        </div>
      ) : null}

      <div className="mx-admin-roles__members">
        {members.map((m) => (
          <RoleMemberCard key={m.id} member={m} />
        ))}
      </div>

      {data ? (
        <div className="mx-admin-roles__membersPager">
          <Pagination
            page={pageNumber}
            totalPages={totalPages}
            onChange={setPageNumber}
            disabled={isLoading}
          />
        </div>
      ) : null}
    </Modal>
  );
}

function RoleMemberCard({ member }: { member: UserListItemResponse }) {
  return (
    <div className="mx-admin-roles__memberCard">
      <div className="mx-admin-roles__memberName">{member.username}</div>
      <div className="mx-admin-roles__memberEmail">{member.email}</div>
      <div className="mx-admin-roles__memberMeta">{member.id}</div>
    </div>
  );
}

function RolePermissionsModal({
  role,
  onClose,
}: {
  role: RoleResponse;
  onClose: () => void;
}) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [catalog, setCatalog] = useState<PermissionCatalogItemResponse[]>([]);
  const [rolePermissions, setRolePermissions] =
    useState<RolePermissionsResponse | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);

    Promise.all([getPermissionsCatalog(), getRolePermissions(role.id)])
      .then(([permissions, rolePerms]) => {
        if (!active) return;
        setCatalog(permissions.filter((p) => !p.isDeprecated));
        setRolePermissions(rolePerms);
      })
      .catch((e) => {
        console.error(e);
        if (!active) return;
        setError("Failed to load role permissions");
      })
      .finally(() => {
        if (!active) return;
        setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [role.id]);

  const assigned = useMemo(
    () => new Set(rolePermissions?.permissionKeys ?? []),
    [rolePermissions]
  );

  return (
    <Modal
      open
      title={`Permissions · ${role.name}`}
      onClose={onClose}
      footer={
        <Button variant="primary" onClick={onClose}>
          Close
        </Button>
      }
    >
      {error ? <div className="mx-admin-roles__error">{error}</div> : null}
      {loading ? (
        <div className="mx-admin-roles__loading">
          <LoadingIndicator label="Loading permissions" />
        </div>
      ) : null}

      <div className="mx-admin-roles__permissions">
        {catalog.map((p) => (
          <div key={p.key} className="mx-admin-roles__permissionRow">
            <div>
              <div className="mx-admin-roles__permissionKey">{p.key}</div>
              <div className="mx-admin-roles__permissionDesc">
                {p.description}
              </div>
            </div>
            <span
              className={
                assigned.has(p.key)
                  ? "mx-admin-roles__permissionBadge is-active"
                  : "mx-admin-roles__permissionBadge"
              }
            >
              {assigned.has(p.key) ? "Granted" : "Not granted"}
            </span>
          </div>
        ))}
      </div>
    </Modal>
  );
}
