import { useState } from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import { useConfirm } from "@shared/ui/components/ConfirmDialog/ConfirmDialog";
import { deleteRole } from "@services/identity/api/admin/adminApi";
import type { RoleResponse } from "@services/identity/api/admin/adminTypes";
import { useAdminRoles } from "../hooks/useAdminRoles";
import RoleCard from "../components/RoleCard";
import CreateRoleModal from "../components/CreateRoleModal";
import RenameRoleModal from "../components/RenameRoleModal";
import RoleMembersModal from "../components/RoleMembersModal";
import RolePermissionsModal from "../components/RolePermissionsModal";
import "../admin-roles-page.css";

export default function AdminRolesPage() {
  const [createOpen, setCreateOpen] = useState(false);
  const [renameRoleTarget, setRenameRoleTarget] = useState<RoleResponse | null>(
    null
  );
  const [membersRole, setMembersRole] = useState<RoleResponse | null>(null);
  const [permissionsRole, setPermissionsRole] = useState<RoleResponse | null>(
    null
  );
  const confirm = useConfirm();

  const { loading, error, roles, setLoading, setError, load } = useAdminRoles();

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
    } catch (error: any) {
      setError(error?.message ?? "Failed to delete role");
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
          {roles.map((role) => (
            <RoleCard
              key={role.id}
              role={role}
              onMembers={setMembersRole}
              onPermissions={setPermissionsRole}
              onRename={setRenameRoleTarget}
              onDelete={handleDelete}
            />
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
