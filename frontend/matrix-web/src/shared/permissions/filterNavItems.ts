import type { NavItem } from "@shared/navigation/Sidebar/types";

type PermissionChecks = {
  canAny: (perms: string[]) => boolean;
  canAll: (perms: string[]) => boolean;
};

export const filterNavItems = (
  items: NavItem[],
  { canAny, canAll }: PermissionChecks,
): NavItem[] => {
  return items.flatMap((item) => {
    if (!item.requiredPermissions || item.requiredPermissions.length === 0) {
      return [item];
    }

    const check = item.requiredPermissionsMode === "all" ? canAll : canAny;
    const allowed = check(item.requiredPermissions);

    if (allowed) return [item];

    if (item.permissionDisplay === "disable") {
      return [
        {
          ...item,
          disabled: true,
          disabledReason: item.disabledReason ?? "Недостаточно прав",
        },
      ];
    }

    return [];
  });
};
