import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import type {
  PermissionCatalogItemResponse,
  RoleResponse,
} from "@services/identity/api/admin/adminTypes";

type PermissionGroup = [string, PermissionCatalogItemResponse[]];

export default function PermissionsMatrix({
  grouped,
  activeRole,
  rolePermissions,
  roleLoading,
  loading,
  onToggle,
}: {
  grouped: PermissionGroup[];
  activeRole: RoleResponse | null;
  rolePermissions: Set<string>;
  roleLoading: boolean;
  loading: boolean;
  onToggle: (key: string) => void;
}) {
  return (
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
              {items.map((permission) => (
                <label key={permission.key} className="mx-admin-perm__row">
                  <div className="mx-admin-perm__permKey">{permission.key}</div>
                  <div className="mx-admin-perm__permDesc">
                    {permission.description}
                  </div>

                  <div className="mx-admin-perm__toggle">
                    <input
                      type="checkbox"
                      checked={rolePermissions.has(permission.key)}
                      disabled={!activeRole || roleLoading || loading}
                      onChange={() => onToggle(permission.key)}
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
  );
}
