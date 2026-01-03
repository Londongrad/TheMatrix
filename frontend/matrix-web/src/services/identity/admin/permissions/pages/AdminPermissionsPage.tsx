import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import { useAdminPermissions } from "../hooks/useAdminPermissions";
import PermissionsMatrix from "../components/PermissionsMatrix";
import RoleList from "../components/RoleList";
import "../admin-permissions-page.css";

export default function AdminPermissionsPage() {
  const {
    loading,
    roleLoading,
    saving,
    error,
    roles,
    activeRoleId,
    setActiveRoleId,
    activeRole,
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
        subtitle="Configure permissions inside roles"
        right={
          <div className="mx-admin-perm__headerRight">
            <Button onClick={() => void load()} disabled={loading}>
              Refresh
            </Button>
            <Button
              variant="primary"
              disabled={!activeRole || !dirty || saving}
              onClick={() => void saveChanges()}
            >
              Save changes
            </Button>
          </div>
        }
      >
        {error ? <div className="mx-admin-perm__error">{error}</div> : null}

        {loading ? (
          <div className="mx-admin-perm__loading">
            <LoadingIndicator label="Loading roles and permissions" />
          </div>
        ) : null}

        <div className="mx-admin-perm__layout">
          <RoleList
            roles={roles}
            activeRoleId={activeRoleId}
            onSelect={setActiveRoleId}
          />
          <PermissionsMatrix
            grouped={grouped}
            activeRole={activeRole}
            rolePermissions={rolePermissions}
            roleLoading={roleLoading}
            loading={loading}
            onToggle={togglePermission}
          />
        </div>
      </Card>
    </div>
  );
}
