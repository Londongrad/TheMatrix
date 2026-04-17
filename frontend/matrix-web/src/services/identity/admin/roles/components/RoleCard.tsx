import Button from "@shared/ui/controls/Button/Button";
import type {RoleResponse} from "@services/identity/api/admin/adminTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";

export default function RoleCard({
                                     role,
                                     isDeleting = false,
                                     onMembers,
                                     onPermissions,
                                     onRename,
                                     onDelete,
                                 }: {
    role: RoleResponse;
    isDeleting?: boolean;
    onMembers: (role: RoleResponse) => void;
    onPermissions: (role: RoleResponse) => void;
    onRename: (role: RoleResponse) => void;
    onDelete: (role: RoleResponse) => void;
}) {
    return (
        <div
            className={
                isDeleting
                    ? "mx-admin-roles__item is-pending"
                    : "mx-admin-roles__item"
            }
        >
            <div className="mx-admin-roles__head">
                <div className="mx-admin-roles__name">{role.name}</div>
                <div className="mx-admin-roles__chips">
                    {role.isSystem ? (
                        <span className="mx-admin-roles__chip">System</span>
                    ) : null}
                    {isDeleting ? (
                        <span className="mx-admin-roles__chip mx-admin-roles__chip--pending">
                            <span
                                className="mx-admin-roles__inlineSpinner"
                                aria-hidden="true"
                            />
                            Deleting...
                        </span>
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

            {isDeleting ? (
                <div className="mx-admin-roles__hint" aria-live="polite">
                    Removing role membership links and refreshing the catalog...
                </div>
            ) : null}

            <div className="mx-admin-roles__actions">
                <RequirePermission
                    perm={PermissionKeys.IdentityRoleMembersRead}
                    displayMode="disable"
                >
                    <Button
                        type="button"
                        onClick={() => onMembers(role)}
                        disabled={isDeleting}
                    >
                        Members
                    </Button>
                </RequirePermission>
                <RequirePermission
                    perm={PermissionKeys.IdentityRolePermissionsRead}
                    displayMode="disable"
                >
                    <Button
                        type="button"
                        onClick={() => onPermissions(role)}
                        disabled={isDeleting}
                    >
                        Permissions
                    </Button>
                </RequirePermission>
                <RequirePermission
                    perm={PermissionKeys.IdentityRolesRename}
                    displayMode="disable"
                >
                    <Button
                        type="button"
                        onClick={() => onRename(role)}
                        disabled={role.isSystem || isDeleting}
                    >
                        Rename
                    </Button>
                </RequirePermission>
                <RequirePermission
                    perm={PermissionKeys.IdentityRolesDelete}
                    displayMode="disable"
                >
                    <Button
                        type="button"
                        variant="danger"
                        onClick={() => onDelete(role)}
                        disabled={role.isSystem || isDeleting}
                        aria-busy={isDeleting}
                    >
                        {isDeleting ? "Deleting..." : "Delete"}
                    </Button>
                </RequirePermission>
            </div>
        </div>
    );
}
