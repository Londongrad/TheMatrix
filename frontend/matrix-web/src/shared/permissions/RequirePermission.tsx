import type {ReactElement} from "react";
import {IconLock} from "@shared/ui/icons/icons";
import type {PermissionMatchMode} from "@shared/permissions/permissionMatchMode";
import {usePermissions} from "./usePermissions";
import "./require-permission.css";

type RequirePermissionProps = {
    perm: string;
    displayMode?: "hide" | "disable";
    tooltip?: string;
    children: ReactElement;
};

type RequirePermissionsProps = {
    perms: string[];
    displayMode?: "hide" | "disable";
    permissionMatchMode?: PermissionMatchMode;
    tooltip?: string;
    children: ReactElement;
};

const renderDisabled = (children: ReactElement, tooltip: string) => (
    <span className="mx-permission is-disabled" title={tooltip}>
        <span className="mx-permission__content" aria-hidden="true">
            {children}
        </span>
        <span className="mx-permission__lock" aria-hidden="true">
            <IconLock/>
        </span>
    </span>
);

export const RequirePermission = ({
                                      perm,
                                      displayMode = "hide",
                                      tooltip = "Not enough permissions",
                                      children,
                                  }: RequirePermissionProps) => {
    const {can} = usePermissions();
    const allowed = can(perm);

    if (allowed) return children;
    if (displayMode === "hide") return null;

    return renderDisabled(children, tooltip);
};

export const RequirePermissions = ({
                                       perms,
                                       displayMode = "hide",
                                       permissionMatchMode = "any",
                                       tooltip = "Not enough permissions",
                                       children,
                                   }: RequirePermissionsProps) => {
    const {canAll, canAny} = usePermissions();
    const allowed = permissionMatchMode === "all" ? canAll(perms) : canAny(perms);

    if (allowed) return children;
    if (displayMode === "hide") return null;

    return renderDisabled(children, tooltip);
};
