import {useEffect, useState} from "react";
import Button from "@shared/ui/controls/Button/Button";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import {usePermissions} from "@shared/permissions/usePermissions";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import type {PermissionScope, PermissionSection} from "../hooks/useAdminPermissions";

type PermissionItem = {
    key: string;
    description: string;
};

export default function PermissionsMatrix({
                                              grouped,
                                              activeScope,
                                              rolePermissions,
                                              roleLoading,
                                              loading,
                                              dirty,
                                              onToggle,
                                          }: {
    grouped: PermissionSection[];
    activeScope: PermissionScope | null;
    rolePermissions: Set<string>;
    roleLoading: boolean;
    loading: boolean;
    dirty: boolean;
    onToggle: (key: string) => void;
}) {
    const {can} = usePermissions();
    const canUpdate = can(PermissionKeys.IdentityRolePermissionsUpdate) && !!activeScope?.editable;
    const [openSections, setOpenSections] = useState<Record<string, boolean>>({});
    const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({});

    useEffect(() => {
        setOpenSections(
            Object.fromEntries(grouped.map((section) => [section.title, true]))
        );
        setOpenGroups(
            Object.fromEntries(
                grouped.flatMap((section) =>
                    section.groups.map((group) => [
                        `${section.title}::${group.title}`,
                        true,
                    ])
                )
            )
        );
    }, [grouped]);

    const anyExpanded =
        Object.values(openSections).some(Boolean) ||
        Object.values(openGroups).some(Boolean);

    const toggleAll = () => {
        const nextOpen = !anyExpanded;

        setOpenSections(
            Object.fromEntries(grouped.map((section) => [section.title, nextOpen]))
        );
        setOpenGroups(
            Object.fromEntries(
                grouped.flatMap((section) =>
                    section.groups.map((group) => [
                        `${section.title}::${group.title}`,
                        nextOpen,
                    ])
                )
            )
        );
    };

    const renderPermissionRow = (permission: PermissionItem) => {
        const isAllowed = rolePermissions.has(permission.key);

        return (
            <label key={permission.key} className="mx-admin-perm__row">
                <div className="mx-admin-perm__permCopy">
                    <div className="mx-admin-perm__permKey">{permission.key}</div>
                    <div className="mx-admin-perm__permDesc">{permission.description}</div>
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
                            disabled={!activeScope || roleLoading || loading || !canUpdate}
                            onChange={() => onToggle(permission.key)}
                            title={
                                canUpdate
                                    ? undefined
                                    : "Permission editing is unavailable for this scope"
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
                        {activeScope ? activeScope.name : "Select a scope"}
                    </div>
                    <div className="mx-admin-perm__matrixSub">
                        {activeScope?.kind === "default-user-access"
                            ? "Grant or deny the mutable baseline inherited by ordinary users."
                            : "Inspect or edit the selected role permissions."}
                    </div>
                </div>
                <div className="mx-admin-perm__matrixActions">
                    <Button onClick={toggleAll} disabled={grouped.length === 0}>
                        {anyExpanded ? "Collapse all" : "Expand all"}
                    </Button>
                    {roleLoading ? (
                        <LoadingIndicator label="Loading scope permissions"/>
                    ) : null}
                </div>
            </div>

            {activeScope ? (
                <div className="mx-admin-perm__scopeBanner">
                    <div className="mx-admin-perm__scopeMeta">
                        <span
                            className={`mx-admin-perm__scopeKind ${
                                activeScope.editable
                                    ? "mx-admin-perm__scopeKind--editable"
                                    : "mx-admin-perm__scopeKind--readonly"
                            }`}
                        >
                            {activeScope.kind === "default-user-access"
                                ? "Baseline"
                                : activeScope.role?.isSystem
                                    ? "System role"
                                    : "Custom role"}
                        </span>
                        {activeScope.version !== null ? (
                            <span className="mx-admin-perm__scopeVersion">
                                Version {activeScope.version}
                            </span>
                        ) : null}
                        {dirty ? (
                            <span className="mx-admin-perm__scopeVersion mx-admin-perm__scopeVersion--dirty">
                                Unsaved
                            </span>
                        ) : null}
                    </div>

                    {activeScope.note ? (
                        <div className="mx-admin-perm__scopeNote">{activeScope.note}</div>
                    ) : null}
                </div>
            ) : null}

            <div className="mx-admin-perm__groups">
                {grouped.map((section) => (
                    <details
                        key={section.title}
                        className="mx-admin-perm__section"
                        open={openSections[section.title] ?? true}
                    >
                        <summary
                            className="mx-admin-perm__sectionTitle"
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
                        <div className="mx-admin-perm__sectionBody">
                            {section.groups.map((group) => {
                                const showGroupTitle =
                                    section.groups.length > 1 || group.title !== "General";
                                const groupKey = `${section.title}::${group.title}`;

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
                                        open={openGroups[groupKey] ?? true}
                                    >
                                        <summary
                                            className="mx-admin-perm__groupTitle"
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
