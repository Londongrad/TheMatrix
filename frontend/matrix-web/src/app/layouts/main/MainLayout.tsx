// src/app/layouts/MainLayout.tsx
import { Outlet } from "react-router-dom";
import { useMemo } from "react";
import ShellLayout from "@shared/ui/layouts/ShellLayout/ShellLayout";
import { mainNavItems } from "@shared/navigation/Items/MainItems";
import { usePermissions } from "@shared/permissions/usePermissions";
import { filterNavItems } from "@shared/permissions/filterNavItems";

const MainLayout = () => {
  const { canAny, canAll } = usePermissions();
  const items = useMemo(
    () => filterNavItems(mainNavItems, { canAny, canAll }),
    [canAny, canAll],
  );

  return (
    <ShellLayout
      title="The Matrix"
      items={items}
      storageKey="main.sidebar.collapsed"
      topbarTitle="City control panel"
      topbarSubtitle="Matrix live operations"
    >
      <Outlet />
    </ShellLayout>
  );
};

export default MainLayout;
