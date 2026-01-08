export type NavItem = {
  to: string;
  label: string;
  end?: boolean;
  icon?: React.ReactNode;

  /** Если нужно прокинуть state (например, для /admin: { from: текущий путь }) */
  getState?: (currentPathname: string) => unknown;
};
