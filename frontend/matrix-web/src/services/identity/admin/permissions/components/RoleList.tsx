import type { RoleResponse } from "@services/identity/api/admin/adminTypes";

export default function RoleList({
  roles,
  activeRoleId,
  onSelect,
}: {
  roles: RoleResponse[];
  activeRoleId: string | null;
  onSelect: (id: string) => void;
}) {
  return (
    <div className="mx-admin-perm__roles">
      <div className="mx-admin-perm__sideTitle">Role</div>
      <div className="mx-admin-perm__roleList">
        {roles.map((role) => (
          <button
            key={role.id}
            type="button"
            className={`mx-admin-perm__roleBtn${
              role.id === activeRoleId ? " is-active" : ""
            }`}
            onClick={() => onSelect(role.id)}
          >
            <div className="mx-admin-perm__roleName">{role.name}</div>
            <div className="mx-admin-perm__roleMeta">
              {role.isSystem ? "System" : "Custom"}
            </div>
          </button>
        ))}
      </div>
    </div>
  );
}
