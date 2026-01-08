// src/app/layouts/MainLayout.tsx
import { Outlet } from "react-router-dom";
import ShellLayout from "@shared/ui/layouts/ShellLayout/ShellLayout";
import { mainNavItems } from "@shared/navigation/Items/MainItems";

const MainLayout = () => {
  return (
    <ShellLayout
      title="The Matrix"
      items={mainNavItems}
      storageKey="main.sidebar.collapsed"
    >
      <Outlet />
    </ShellLayout>
  );
};

export default MainLayout;
