import type { ReactElement } from "react";
import { IconLock } from "@shared/ui/icons/icons";
import { usePermissions } from "./usePermissions";
import "./require-permission.css";

type RequirePermissionProps = {
  perm: string;
  mode?: "hide" | "disable";
  tooltip?: string;
  children: ReactElement;
};

type RequirePermissionsProps = {
  perms: string[];
  mode?: "hide" | "disable";
  match?: "any" | "all";
  tooltip?: string;
  children: ReactElement;
};

const renderDisabled = (children: ReactElement, tooltip: string) => (
  <span className="mx-permission is-disabled" title={tooltip}>
    <span className="mx-permission__content" aria-hidden="true">
      {children}
    </span>
    <span className="mx-permission__lock" aria-hidden="true">
      <IconLock />
    </span>
  </span>
);

export const RequirePermission = ({
  perm,
  mode = "hide",
  tooltip = "Недостаточно прав",
  children,
}: RequirePermissionProps) => {
  const { can } = usePermissions();
  const allowed = can(perm);

  if (allowed) return children;
  if (mode === "hide") return null;

  return renderDisabled(children, tooltip);
};

export const RequirePermissions = ({
  perms,
  mode = "hide",
  match = "any",
  tooltip = "Недостаточно прав",
  children,
}: RequirePermissionsProps) => {
  const { canAll, canAny } = usePermissions();
  const allowed = match === "all" ? canAll(perms) : canAny(perms);

  if (allowed) return children;
  if (mode === "hide") return null;

  return renderDisabled(children, tooltip);
};
