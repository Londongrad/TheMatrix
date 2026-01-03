import Button from "@shared/ui/controls/Button/Button";
import type { RoleResponse } from "@services/identity/api/admin/adminTypes";

export default function RoleCard({
  role,
  onMembers,
  onPermissions,
  onRename,
  onDelete,
}: {
  role: RoleResponse;
  onMembers: (role: RoleResponse) => void;
  onPermissions: (role: RoleResponse) => void;
  onRename: (role: RoleResponse) => void;
  onDelete: (role: RoleResponse) => void;
}) {
  return (
    <div className="mx-admin-roles__item">
      <div className="mx-admin-roles__head">
        <div className="mx-admin-roles__name">{role.name}</div>
        <div className="mx-admin-roles__chips">
          {role.isSystem ? (
            <span className="mx-admin-roles__chip">System</span>
          ) : null}
        </div>
      </div>

      <div className="mx-admin-roles__meta">
        <span className="mx-admin-roles__mono">{role.id}</span>
        <span className="mx-admin-roles__muted">•</span>
        <span className="mx-admin-roles__muted">
          {role.createdAtUtc.replace("T", " ").replace("Z", "")}
        </span>
      </div>

      <div className="mx-admin-roles__actions">
        <Button type="button" onClick={() => onMembers(role)}>
          Members
        </Button>
        <Button type="button" onClick={() => onPermissions(role)}>
          Permissions
        </Button>
        <Button
          type="button"
          onClick={() => onRename(role)}
          disabled={role.isSystem}
        >
          Rename
        </Button>
        <Button
          type="button"
          variant="danger"
          onClick={() => onDelete(role)}
          disabled={role.isSystem}
        >
          Delete
        </Button>
      </div>
    </div>
  );
}
