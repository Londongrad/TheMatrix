import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import type {RoleResponse} from "@services/identity/api/admin/adminTypes";
import {usePermissions} from "@shared/permissions/usePermissions";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import type {PermissionSection} from "../hooks/useAdminPermissions";

type PermissionItem = {
    key: string;
    description: string;
};

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
    const {can} = usePermissions();
    const canUpdate = can(PermissionKeys.IdentityRolePermissionsUpdate);

    const renderPermissionRow = (permission: PermissionItem) => {
        const isAllowed = rolePermissions.has(permission.key);

        return (
            <label
                key={permission.key}
                className="mx-admin-perm__row"
            >
                <div className="mx-admin-perm__permCopy">
                    <div className="mx-admin-perm__permKey">
                        {permission.key}
                    </div>
                    <div className="mx-admin-perm__permDesc">
                        {permission.description}
                    </div>
                </div>

                <div className="mx-admin-perm__toggle">
                    <span
                        className={`mx-admin-perm__toggleState ${
                            isAllowed
                                ? "mx-admin-perm__toggleState--allow"
                                : "mx-admin-perm__toggleState--deny"
                        }`}
                    >
                        {isAllowed ? "Allow" : "Deny"}
                    </span>

                    <span className="mx-admin-perm__toggleSwitch">
                        <input
                            type="checkbox"
                            checked={isAllowed}
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
                        <span/>
                    </span>
                </div>
            </label>
        );
    };

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
                    <LoadingIndicator label="Loading role permissions"/>
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
                                                {group.items.map(renderPermissionRow)}
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
                                            {group.items.map(renderPermissionRow)}
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
