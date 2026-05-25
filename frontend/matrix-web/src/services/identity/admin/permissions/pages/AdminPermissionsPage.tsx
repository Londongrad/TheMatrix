import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {DEFAULT_USER_ACCESS_SCOPE_ID, useAdminPermissions} from "../hooks/useAdminPermissions";
import PermissionsMatrix from "../components/PermissionsMatrix";
import RoleList from "../components/RoleList";

import "../styles/admin-permissions-page.css";

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

    const editableScopesCount = scopes.filter((scope) => scope.editable).length;
    const activeGrantedCount = rolePermissions.size;
    const permissionsCount = grouped.reduce(
        (sectionTotal, section) =>
            sectionTotal +
            section.groups.reduce(
                (groupTotal, group) => groupTotal + group.items.length,
                0,
            ),
        0,
    );

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

                <div className="mx-admin-perm__overview">
                    <div className="mx-admin-perm__overviewHero">
                        <div className="mx-admin-perm__overviewEyebrow">
                            Identity watchboard
                        </div>
                        <div className="mx-admin-perm__overviewTitle">
                            {activeScope
                                ? `Inspecting ${activeScope.name}`
                                : "Select a permission scope"}
                        </div>
                        <div className="mx-admin-perm__overviewText">
                            {activeScope?.editable
                                ? "Mutable scopes can be tuned live. The matrix below becomes the operational baseline for future authorization snapshots."
                                : "Read-only scopes stay visible as system blueprints so you can compare seeded grants against the mutable user baseline."}
                        </div>
                    </div>

                    <div className="mx-admin-perm__overviewStats">
                        <div className="mx-admin-perm__overviewStat">
                            <span className="mx-admin-perm__overviewStatValue">
                                {scopes.length}
                            </span>
                            <span className="mx-admin-perm__overviewStatLabel">
                                Total scopes
                            </span>
                        </div>
                        <div className="mx-admin-perm__overviewStat">
                            <span className="mx-admin-perm__overviewStatValue">
                                {editableScopesCount}
                            </span>
                            <span className="mx-admin-perm__overviewStatLabel">
                                Mutable scopes
                            </span>
                        </div>
                        <div className="mx-admin-perm__overviewStat">
                            <span className="mx-admin-perm__overviewStatValue">
                                {permissionsCount}
                            </span>
                            <span className="mx-admin-perm__overviewStatLabel">
                                Catalog permissions
                            </span>
                        </div>
                        <div className="mx-admin-perm__overviewStat">
                            <span className="mx-admin-perm__overviewStatValue">
                                {activeGrantedCount}
                            </span>
                            <span className="mx-admin-perm__overviewStatLabel">
                                Active grants
                            </span>
                        </div>
                    </div>
                </div>

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
