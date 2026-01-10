import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import type { RoleResponse } from "@services/identity/api/admin/adminTypes";
import { usePermissions } from "@shared/permissions/usePermissions";
import { PermissionKeys } from "@shared/permissions/permissionKeys";
import type { PermissionSection } from "../hooks/useAdminPermissions";

export default function PermissionsMatrix({
  grouped,
  activeRole,
  rolePermissions,
  roleLoading,
  loading,
  onToggle,
}: {
  grouped: PermissionSection[];
  activeRole: RoleResponse | null;
  rolePermissions: Set<string>;
  roleLoading: boolean;
  loading: boolean;
  onToggle: (key: string) => void;
}) {
  const { can } = usePermissions();
  const canUpdate = can(PermissionKeys.IdentityRolePermissionsUpdate);

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
        {grouped.map((section) => (
          <details key={section.title} className="mx-admin-perm__section" open>
            <summary className="mx-admin-perm__sectionTitle">
              {section.title}
            </summary>
            <div className="mx-admin-perm__sectionBody">
              {section.groups.map((group) => {
                const showGroupTitle =
                  section.groups.length > 1 || group.title !== "General";

                if (!showGroupTitle) {
                  return (
                    <div key={group.title} className="mx-admin-perm__groupBody">
                      <div className="mx-admin-perm__rows">
                        {group.items.map((permission) => (
                          <label
                            key={permission.key}
                            className="mx-admin-perm__row"
                          >
                            <div className="mx-admin-perm__permKey">
                              {permission.key}
                            </div>
                            <div className="mx-admin-perm__permDesc">
                              {permission.description}
                            </div>

                            <div className="mx-admin-perm__toggle">
                              <input
                                type="checkbox"
                                checked={rolePermissions.has(permission.key)}
                                disabled={
                                  !activeRole ||
                                  roleLoading ||
                                  loading ||
                                  !canUpdate
                                }
                                onChange={() => onToggle(permission.key)}
                                title={
                                  canUpdate ? undefined : "Недостаточно прав"
                                }
                              />
                              <span />
                            </div>
                          </label>
                        ))}
                      </div>
                    </div>
                  );
                }

                return (
                  <details
                    key={group.title}
                    className="mx-admin-perm__group"
                    open
                  >
                    <summary className="mx-admin-perm__groupTitle">
                      {group.title}
                    </summary>
                    <div className="mx-admin-perm__rows">
                      {group.items.map((permission) => (
                        <label
                          key={permission.key}
                          className="mx-admin-perm__row"
                        >
                          <div className="mx-admin-perm__permKey">
                            {permission.key}
                          </div>
                          <div className="mx-admin-perm__permDesc">
                            {permission.description}
                          </div>

                          <div className="mx-admin-perm__toggle">
                            <input
                              type="checkbox"
                              checked={rolePermissions.has(permission.key)}
                              disabled={
                                !activeRole ||
                                roleLoading ||
                                loading ||
                                !canUpdate
                              }
                              onChange={() => onToggle(permission.key)}
                              title={
                                canUpdate ? undefined : "Недостаточно прав"
                              }
                            />
                            <span />
                          </div>
                        </label>
                      ))}
                    </div>
                  </details>
                );
              })}
            </div>
          </details>
        ))}
      </div>
    </div>
  );
}
