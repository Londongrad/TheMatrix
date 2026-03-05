import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Modal from "@shared/ui/components/Modal/Modal";
import {
    formatAdminRelativeVisit,
    formatAdminUtc,
    formatAdminVisitUtc,
} from "@services/identity/admin/shared/utils/dateTime";
import UserBadge from "./UserBadge";
import {useUserAccess} from "../hooks/useUserAccess";

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
        permissionsCatalog,
        permissionMap,
        rolePermissionKeys,
        selectedRoleIds,
        setSelectedRoleIds,
        saveRoles,
        updatePermission,
        isDeletedUser,
        isAccessReadOnly,
        readOnlyReason,
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
                    <LoadingIndicator label="Loading access details"/>
                </div>
            ) : null}

            {error ? <div className="mx-admin-users__error">{error}</div> : null}
            {readOnlyReason ? (
                <div className="mx-admin-users__muted">{readOnlyReason}</div>
            ) : null}

            {details ? (
                <div className="mx-admin-users__modal">
                    <div className="mx-admin-users__section">
                        <div className="mx-admin-users__sectionTitle">Profile</div>
                        <div className="mx-admin-users__profileGrid">
                            <div className="mx-admin-users__profileItem">
                                <div className="mx-admin-users__muted">Username</div>
                                <div className="mx-admin-users__profileValue">{details.username}</div>
                            </div>
                            <div className="mx-admin-users__profileItem">
                                <div className="mx-admin-users__muted">Email</div>
                                <div className="mx-admin-users__profileValue">{details.email}</div>
                            </div>
                            <div className="mx-admin-users__profileItem">
                                <div className="mx-admin-users__muted">Permissions version</div>
                                <div className="mx-admin-users__profileValue">{details.permissionsVersion}</div>
                            </div>
                            <div className="mx-admin-users__profileItem">
                                <div className="mx-admin-users__muted">Status</div>
                                <div className="mx-admin-users__profileValue">
                                    {details.isDeleted ? (
                                        <UserBadge kind="bad">Deleted</UserBadge>
                                    ) : details.isLocked ? (
                                        <UserBadge kind="warn">Locked</UserBadge>
                                    ) : (
                                        <UserBadge kind="ok">Active</UserBadge>
                                    )}
                                </div>
                            </div>
                            <div className="mx-admin-users__profileItem">
                                <div className="mx-admin-users__muted">Created</div>
                                <div className="mx-admin-users__profileValue">
                                    {formatAdminUtc(details.createdAtUtc)}
                                </div>
                            </div>
                            {details.deletedAtUtc ? (
                                <div className="mx-admin-users__profileItem">
                                    <div className="mx-admin-users__muted">Deleted at</div>
                                    <div className="mx-admin-users__profileValue">
                                        {formatAdminUtc(details.deletedAtUtc)}
                                    </div>
                                </div>
                            ) : null}
                            <div className="mx-admin-users__profileItem">
                                <div className="mx-admin-users__muted">Last visit</div>
                                <div
                                    className="mx-admin-users__time mx-admin-users__time--profile"
                                    title={formatAdminVisitUtc(details.lastVisitedAtUtc)}
                                >
                                    <span className="mx-admin-users__timePrimary">
                                        {formatAdminRelativeVisit(details.lastVisitedAtUtc)}
                                    </span>
                                    {details.lastVisitedAtUtc ? (
                                        <span className="mx-admin-users__timeSecondary">
                                            {formatAdminVisitUtc(details.lastVisitedAtUtc)}
                                        </span>
                                    ) : null}
                                </div>
                            </div>
                            {isDeletedUser ? (
                                <div className="mx-admin-users__profileItem mx-admin-users__profileItem--full">
                                    <div className="mx-admin-users__muted">
                                        Restore this account from the users list before changing roles or permission overrides.
                                    </div>
                                </div>
                            ) : null}
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
                                        disabled={savingRoles || isAccessReadOnly}
                                        onChange={(event) => {
                                            if (event.target.checked) {
                                                setSelectedRoleIds((prev) =>
                                                    prev.includes(role.id)
                                                        ? prev
                                                        : [...prev, role.id]
                                                );
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
                            <Button
                                onClick={() => void saveRoles()}
                                disabled={savingRoles || isAccessReadOnly}
                            >
                                Save roles
                            </Button>
                            <div className="mx-admin-users__muted">
                                {rolesCatalog.filter((role) => selectedRoleIds.includes(role.id)).length} assigned
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
                                    isAccessReadOnly ||
                                    savingPermission === permission.key ||
                                    effectiveEffect === "Allow";
                                const denyDisabled =
                                    isAccessReadOnly ||
                                    savingPermission === permission.key ||
                                    effectiveEffect === "Deny";
                                return (
                                    <div
                                        key={permission.key}
                                        className="mx-admin-users__permissionRow"
                                    >
                                        <div className="mx-admin-users__permCopy">
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
                                                variant="success"
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
