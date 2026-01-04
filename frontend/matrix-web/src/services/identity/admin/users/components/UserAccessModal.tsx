import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Modal from "@shared/ui/components/Modal/Modal";
import UserBadge from "./UserBadge";
import { useUserAccess } from "../hooks/useUserAccess";

function formatUtc(utc: string) {
  return utc?.replace("T", " ").replace("Z", "");
}

export default function UserAccessModal({
  userId,
  onClose,
}: {
  userId: string;
  onClose: () => void;
}) {
  const {
    loading,
    savingRoles,
    savingPermission,
    error,
    details,
    rolesCatalog,
    userRoles,
    permissionsCatalog,
    permissionMap,
    rolePermissionKeys,
    selectedRoleIds,
    setSelectedRoleIds,
    saveRoles,
    updatePermission,
  } = useUserAccess(userId);

  return (
    <Modal
      open
      title="User access"
      onClose={onClose}
      footer={
        <Button variant="primary" onClick={onClose}>
          Close
        </Button>
      }
    >
      {loading ? (
        <div className="mx-admin-users__loading">
          <LoadingIndicator label="Loading access details" />
        </div>
      ) : null}

      {error ? <div className="mx-admin-users__error">{error}</div> : null}

      {details ? (
        <div className="mx-admin-users__modal">
          <div className="mx-admin-users__section">
            <div className="mx-admin-users__sectionTitle">Profile</div>
            <div className="mx-admin-users__profileGrid">
              <div>
                <div className="mx-admin-users__muted">Username</div>
                <div>{details.username}</div>
              </div>
              <div>
                <div className="mx-admin-users__muted">Email</div>
                <div>{details.email}</div>
              </div>
              <div>
                <div className="mx-admin-users__muted">Permissions version</div>
                <div>{details.permissionsVersion}</div>
              </div>
              <div>
                <div className="mx-admin-users__muted">Created</div>
                <div>{formatUtc(details.createdAtUtc)}</div>
              </div>
            </div>
          </div>

          <div className="mx-admin-users__section">
            <div className="mx-admin-users__sectionTitle">Roles</div>
            <div className="mx-admin-users__roles">
              {rolesCatalog.map((role) => (
                <label key={role.id} className="mx-admin-users__roleItem">
                  <input
                    type="checkbox"
                    checked={selectedRoleIds.includes(role.id)}
                    onChange={(event) => {
                      if (event.target.checked) {
                        setSelectedRoleIds((prev) => [...prev, role.id]);
                      } else {
                        setSelectedRoleIds((prev) =>
                          prev.filter((id) => id !== role.id)
                        );
                      }
                    }}
                  />
                  <span>{role.name}</span>
                  {role.isSystem ? (
                    <span className="mx-admin-users__chip">System</span>
                  ) : null}
                </label>
              ))}
            </div>
            <div className="mx-admin-users__rolesActions">
              <Button onClick={() => void saveRoles()} disabled={savingRoles}>
                Save roles
              </Button>
              <div className="mx-admin-users__muted">
                {userRoles.length} assigned
              </div>
            </div>
          </div>

          <div className="mx-admin-users__section">
            <div className="mx-admin-users__sectionTitle">
              Direct permission overrides
            </div>
            <div className="mx-admin-users__permissions">
              {permissionsCatalog.map((permission) => {
                const override = permissionMap.get(permission.key);
                const hasRolePermission = rolePermissionKeys.has(
                  permission.key
                );
                const effectiveEffect =
                  override?.effect ?? (hasRolePermission ? "Allow" : "None");
                const badgeKind =
                  effectiveEffect === "Allow"
                    ? "ok"
                    : effectiveEffect === "Deny"
                    ? "bad"
                    : "warn";
                const allowDisabled =
                  savingPermission === permission.key ||
                  effectiveEffect === "Allow";
                const denyDisabled =
                  savingPermission === permission.key ||
                  effectiveEffect === "Deny";
                return (
                  <div
                    key={permission.key}
                    className="mx-admin-users__permissionRow"
                  >
                    <div>
                      <div className="mx-admin-users__permKey">
                        {permission.key}
                      </div>
                      <div className="mx-admin-users__permDesc">
                        {permission.description}
                      </div>
                    </div>
                    <div className="mx-admin-users__permActions">
                      <UserBadge kind={badgeKind}>{effectiveEffect}</UserBadge>
                      {override ? (
                        <UserBadge kind="info">Manual</UserBadge>
                      ) : hasRolePermission ? (
                        <UserBadge kind="info">Role</UserBadge>
                      ) : null}
                      <Button
                        size="sm"
                        disabled={allowDisabled}
                        onClick={() =>
                          void updatePermission(permission.key, "Allow")
                        }
                      >
                        Allow
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        disabled={denyDisabled}
                        onClick={() =>
                          void updatePermission(permission.key, "Deny")
                        }
                      >
                        Deny
                      </Button>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      ) : null}
    </Modal>
  );
}
