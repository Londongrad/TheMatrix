import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {useAdminPermissions} from "../hooks/useAdminPermissions";
import PermissionsMatrix from "../components/PermissionsMatrix";
import RoleList from "../components/RoleList";

import "../styles/admin-permissions-page.css";
import {DEFAULT_USER_ACCESS_SCOPE_ID} from "../hooks/useAdminPermissions";

export default function AdminPermissionsPage() {
    const {
        loading,
        roleLoading,
        saving,
        error,
        scopes,
        activeScopeId,
        setActiveScopeId,
        activeScope,
        grouped,
        rolePermissions,
        dirty,
        load,
        togglePermission,
        saveChanges,
    } = useAdminPermissions();

    return (
        <div className="mx-admin-page">
            <Card
                title="Permissions"
                subtitle="Configure the default user baseline and inspect role grants"
                right={
                    <div className="mx-admin-perm__headerRight">
                        <RequirePermission
                            perm={PermissionKeys.IdentityPermissionsCatalogRead}
                            displayMode="disable"
                        >
                            <Button onClick={() => void load()} disabled={loading}>
                                Refresh
                            </Button>
                        </RequirePermission>
                        <RequirePermission
                            perm={PermissionKeys.IdentityRolePermissionsUpdate}
                            displayMode="disable"
                        >
                            <span>
                                {activeScope?.editable ? (
                                    <Button
                                        variant="primary"
                                        disabled={!dirty || saving}
                                        onClick={() => void saveChanges()}
                                    >
                                        {activeScope.kind === "default-user-access"
                                            ? "Save baseline"
                                            : "Save changes"}
                                    </Button>
                                ) : activeScope?.role?.name === "User" ? (
                                    <Button
                                        variant="primary"
                                        onClick={() => setActiveScopeId(DEFAULT_USER_ACCESS_SCOPE_ID)}
                                    >
                                        Open default user access
                                    </Button>
                                ) : (
                                    <></>
                                )}
                            </span>
                        </RequirePermission>
                    </div>
                }
            >
                {error ? <div className="mx-admin-perm__error">{error}</div> : null}

                {loading ? (
                    <div className="mx-admin-perm__loading">
                        <LoadingIndicator label="Loading permission scopes"/>
                    </div>
                ) : null}

                <div className="mx-admin-perm__layout">
                    <RoleList
                        scopes={scopes}
                        activeScopeId={activeScopeId}
                        onSelect={setActiveScopeId}
                    />
                    <PermissionsMatrix
                        grouped={grouped}
                        activeScope={activeScope}
                        rolePermissions={rolePermissions}
                        roleLoading={roleLoading}
                        loading={loading}
                        dirty={dirty}
                        onOpenDefaultUserAccess={() => setActiveScopeId(DEFAULT_USER_ACCESS_SCOPE_ID)}
                        onToggle={togglePermission}
                    />
                </div>
            </Card>
        </div>
    );
}
