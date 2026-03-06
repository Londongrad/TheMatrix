import {useEffect, useState} from "react";
import type {PermissionCatalogItemResponse, PermissionEffect} from "@services/identity/api/admin/adminTypes";
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

type ManualPermissionState = PermissionEffect | "Inherit";

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
        savingPermissions,
        isEditingPermissions,
        error,
        details,
        rolesCatalog,
        permissionMap,
        permissionDraftMap,
        groupedPermissions,
        rolePermissionKeys,
        selectedRoleIds,
        setSelectedRoleIds,
        saveRoles,
        beginPermissionEditing,
        cancelPermissionEditing,
        setPermissionOverride,
        savePermissions,
        hasPermissionChanges,
        pendingPermissionChangesCount,
        canEditPermissionOverrides,
        permissionEditReason,
        isDeletedUser,
        isAccessReadOnly,
        readOnlyReason,
    } = useUserAccess(userId);
    const [openSections, setOpenSections] = useState<Record<string, boolean>>({});
    const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({});

    useEffect(() => {
        setOpenSections(
            Object.fromEntries(groupedPermissions.map((section) => [section.title, true]))
        );
        setOpenGroups(
            Object.fromEntries(
                groupedPermissions.flatMap((section) =>
                    section.groups.map((group) => [
                        `${section.title}::${group.title}`,
                        true,
                    ])
                )
            )
        );
    }, [groupedPermissions]);

    const anyPermissionsExpanded =
        Object.values(openSections).some(Boolean) ||
        Object.values(openGroups).some(Boolean);

    const toggleAllPermissionSections = () => {
        const nextOpen = !anyPermissionsExpanded;

        setOpenSections(
            Object.fromEntries(
                groupedPermissions.map((section) => [section.title, nextOpen])
            )
        );
        setOpenGroups(
            Object.fromEntries(
                groupedPermissions.flatMap((section) =>
                    section.groups.map((group) => [
                        `${section.title}::${group.title}`,
                        nextOpen,
                    ])
                )
            )
        );
    };

    const renderPermissionRow = (permission: PermissionCatalogItemResponse) => {
        const override = permissionMap.get(permission.key);
        const draftOverride = permissionDraftMap[permission.key];
        const selectedState: ManualPermissionState = draftOverride ?? "Inherit";
        const hasRolePermission = rolePermissionKeys.has(permission.key);
        const effectiveEffect = draftOverride ?? (hasRolePermission ? "Allow" : "None");
        const badgeKind =
            effectiveEffect === "Allow"
                ? "ok"
                : effectiveEffect === "Deny"
                    ? "bad"
                    : "warn";
        const sourceLabel = draftOverride
            ? "Manual"
            : hasRolePermission
                ? "Role"
                : "Default";
        const isDirty = (override?.effect ?? null) !== (draftOverride ?? null);
        const editorDisabled =
            !isEditingPermissions ||
            savingPermissions ||
            isAccessReadOnly ||
            !canEditPermissionOverrides;

        return (
            <div key={permission.key} className="mx-admin-users__permissionRow">
                <div className="mx-admin-users__permCopy">
                    <div className="mx-admin-users__permKey">{permission.key}</div>
                    <div className="mx-admin-users__permDesc">{permission.description}</div>
                </div>
                <div className="mx-admin-users__permActions">
                    <div className="mx-admin-users__permBadges">
                        <UserBadge kind={badgeKind}>{effectiveEffect}</UserBadge>
                        <UserBadge kind="info">{sourceLabel}</UserBadge>
                        {isDirty ? <UserBadge kind="warn">Draft</UserBadge> : null}
                    </div>
                    {isEditingPermissions ? (
                        <div className="mx-admin-users__permEditor">
                            <Button
                                size="sm"
                                variant="primary"
                                disabled={editorDisabled || selectedState === "Inherit"}
                                onClick={() =>
                                    setPermissionOverride(permission.key, "Inherit")
                                }
                            >
                                Inherit
                            </Button>
                            <Button
                                size="sm"
                                variant="success"
                                disabled={editorDisabled || selectedState === "Allow"}
                                onClick={() =>
                                    setPermissionOverride(permission.key, "Allow")
                                }
                            >
                                Allow
                            </Button>
                            <Button
                                size="sm"
                                variant="danger"
                                disabled={editorDisabled || selectedState === "Deny"}
                                onClick={() =>
                                    setPermissionOverride(permission.key, "Deny")
                                }
                            >
                                Deny
                            </Button>
                        </div>
                    ) : null}
                </div>
            </div>
        );
    };

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
                        <div className="mx-admin-users__sectionHeader">
                            <div>
                                <div className="mx-admin-users__sectionTitle">
                                    Direct permission overrides
                                </div>
                                <div className="mx-admin-users__muted">
                                    Draft manual allow or deny decisions locally, then save them in one batch.
                                </div>
                            </div>
                            <div className="mx-admin-users__sectionActions">
                                <Button
                                    onClick={toggleAllPermissionSections}
                                    disabled={groupedPermissions.length === 0}
                                >
                                    {anyPermissionsExpanded ? "Collapse all" : "Expand all"}
                                </Button>
                                {isEditingPermissions ? (
                                    <>
                                        <div className="mx-admin-users__muted">
                                            {pendingPermissionChangesCount === 0
                                                ? "No unsaved changes"
                                                : `${pendingPermissionChangesCount} unsaved override${pendingPermissionChangesCount === 1 ? "" : "s"}`}
                                        </div>
                                        <Button
                                            onClick={cancelPermissionEditing}
                                            disabled={savingPermissions}
                                        >
                                            Cancel
                                        </Button>
                                        <Button
                                            variant="primary"
                                            onClick={() => void savePermissions()}
                                            disabled={
                                                savingPermissions ||
                                                !hasPermissionChanges ||
                                                isAccessReadOnly ||
                                                !canEditPermissionOverrides
                                            }
                                            title={permissionEditReason ?? undefined}
                                        >
                                            Save overrides
                                        </Button>
                                    </>
                                ) : (
                                    <Button
                                        onClick={beginPermissionEditing}
                                        disabled={
                                            savingPermissions ||
                                            isAccessReadOnly ||
                                            !canEditPermissionOverrides
                                        }
                                        title={permissionEditReason ?? undefined}
                                    >
                                        Edit overrides
                                    </Button>
                                )}
                            </div>
                        </div>
                        {permissionEditReason ? (
                            <div className="mx-admin-users__muted">
                                {permissionEditReason}
                            </div>
                        ) : null}
                        <div className="mx-admin-users__permissionSections">
                            {groupedPermissions.map((section) => (
                                <details
                                    key={section.title}
                                    className="mx-admin-users__permissionSection"
                                    open={openSections[section.title] ?? true}
                                >
                                    <summary
                                        className="mx-admin-users__permissionSectionTitle"
                                        onClick={(event) => {
                                            event.preventDefault();
                                            setOpenSections((prev) => ({
                                                ...prev,
                                                [section.title]: !(prev[section.title] ?? true),
                                            }));
                                        }}
                                    >
                                        {section.title}
                                    </summary>
                                    <div className="mx-admin-users__permissionSectionBody">
                                        {section.groups.map((group) => {
                                            const showGroupTitle =
                                                section.groups.length > 1 || group.title !== "General";
                                            const groupKey = `${section.title}::${group.title}`;

                                            if (!showGroupTitle) {
                                                return (
                                                    <div
                                                        key={group.title}
                                                        className="mx-admin-users__permissionGroupBody"
                                                    >
                                                        <div className="mx-admin-users__permissions">
                                                            {group.items.map(renderPermissionRow)}
                                                        </div>
                                                    </div>
                                                );
                                            }

                                            return (
                                                <details
                                                    key={group.title}
                                                    className="mx-admin-users__permissionGroup"
                                                    open={openGroups[groupKey] ?? true}
                                                >
                                                    <summary
                                                        className="mx-admin-users__permissionGroupTitle"
                                                        onClick={(event) => {
                                                            event.preventDefault();
                                                            setOpenGroups((prev) => ({
                                                                ...prev,
                                                                [groupKey]: !(prev[groupKey] ?? true),
                                                            }));
                                                        }}
                                                    >
                                                        {group.title}
                                                    </summary>
                                                    <div className="mx-admin-users__permissions">
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
                </div>
            ) : null}
        </Modal>
    );
}
