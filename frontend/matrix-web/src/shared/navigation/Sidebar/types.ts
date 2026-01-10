export type NavItem = {
  to: string;
  label: string;
  end?: boolean;
  icon?: React.ReactNode;
  requiredPermissions?: string[];
  requiredPermissionsMode?: "any" | "all";
  permissionDisplay?: "hide" | "disable";
  disabled?: boolean;
  disabledReason?: string;

  /** Если нужно прокинуть state (например, для /admin: { from: текущий путь }) */
  getState?: (currentPathname: string) => unknown;
};
