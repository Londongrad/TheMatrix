import {useMemo, useState} from "react";
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
                                              onOpenDefaultUserAccess,
                                              onToggle,
                                          }: {
    grouped: PermissionSection[];
    activeScope: PermissionScope | null;
    rolePermissions: Set<string>;
    roleLoading: boolean;
    loading: boolean;
    dirty: boolean;
    onOpenDefaultUserAccess: () => void;
    onToggle: (key: string) => void;
}) {
    const {can} = usePermissions();
    const canUpdate = can(PermissionKeys.IdentityRolePermissionsUpdate) && !!activeScope?.editable;
    const [sectionExpansionState, setSectionExpansionState] = useState<Record<string, boolean>>({});
    const [groupExpansionState, setGroupExpansionState] = useState<Record<string, boolean>>({});
    const openSections = useMemo(
        () => Object.fromEntries(
            grouped.map((section) => [section.title, sectionExpansionState[section.title] ?? true]),
        ),
        [grouped, sectionExpansionState],
    );
    const openGroups = useMemo(
        () => Object.fromEntries(
            grouped.flatMap((section) =>
                section.groups.map((group) => {
                    const key = `${section.title}::${group.title}`;
                    return [key, groupExpansionState[key] ?? true];
                }),
            ),
        ),
        [groupExpansionState, grouped],
    );

    const anyExpanded =
        Object.values(openSections).some(Boolean) ||
        Object.values(openGroups).some(Boolean);

    const toggleAll = () => {
        const nextOpen = !anyExpanded;

        setSectionExpansionState(
            Object.fromEntries(grouped.map((section) => [section.title, nextOpen]))
        );
        setGroupExpansionState(
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

        if (!activeScope?.editable) {
            return (
                <div key={permission.key} className="mx-admin-perm__row mx-admin-perm__row--readonly">
                    <div className="mx-admin-perm__permCopy">
                        <div className="mx-admin-perm__permKey">{permission.key}</div>
                        <div className="mx-admin-perm__permDesc">{permission.description}</div>
                    </div>

                    <div className="mx-admin-perm__readonlyBadgeWrap">
                        <span
                            className={`mx-admin-perm__toggleState ${
                                isAllowed
                                    ? "mx-admin-perm__toggleState--allow"
                                    : "mx-admin-perm__toggleState--deny"
                            }`}
                        >
                            {isAllowed ? "Granted" : "Not granted"}
                        </span>
                    </div>
                </div>
            );
        }

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
                    {activeScope ? (
                        <span className="mx-admin-perm__matrixMetric">
                            {rolePermissions.size} grant{rolePermissions.size === 1 ? "" : "s"}
                        </span>
                    ) : null}
                    <Button onClick={toggleAll} disabled={grouped.length === 0}>
                        {anyExpanded ? "Collapse all" : "Expand all"}
                    </Button>
                    {!activeScope?.editable && activeScope?.role?.name === "User" ? (
                        <Button variant="primary" onClick={onOpenDefaultUserAccess}>
                            Open default user access
                        </Button>
                    ) : null}
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
                        {activeScope.editable ? (
                            <span className="mx-admin-perm__scopeVersion mx-admin-perm__scopeVersion--live">
                                Live editable
                            </span>
                        ) : null}
                    </div>

                    {activeScope.note ? (
                        <div className="mx-admin-perm__scopeNote">{activeScope.note}</div>
                    ) : null}

                    {!activeScope.editable ? (
                        <div className="mx-admin-perm__readonlyPanel">
                            <div className="mx-admin-perm__readonlyArtwork" aria-hidden="true">
                                <span className="mx-admin-perm__readonlyOrb"/>
                                <span className="mx-admin-perm__readonlyGrid"/>
                            </div>
                            <div className="mx-admin-perm__readonlyCopy">
                                <div className="mx-admin-perm__readonlyTitle">
                                    {activeScope.role?.name === "User"
                                        ? "System role blueprint"
                                        : "Immutable system role"}
                                </div>
                                <div className="mx-admin-perm__readonlyText">
                                    {activeScope.role?.name === "User"
                                        ? "The built-in User role stays seeded and read-only. Change the ordinary-user experience through Default user access, then use this screen to inspect the resulting baseline blueprint."
                                        : "This role is intentionally locked so the platform always has a stable administrative blueprint. You can inspect its grants here, but edits stay disabled by design."}
                                </div>
                            </div>
                            {activeScope.role?.name === "User" ? (
                                <div className="mx-admin-perm__readonlyActions">
                                    <Button variant="primary" onClick={onOpenDefaultUserAccess}>
                                        Edit default user access
                                    </Button>
                                </div>
                            ) : null}
                        </div>
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
                                setSectionExpansionState((prev) => ({
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
                                                setGroupExpansionState((prev) => ({
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
