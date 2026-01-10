import { Navigate, useLocation } from "react-router-dom";
import type { ReactElement } from "react";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import { LoadingScreen } from "@services/identity/self/auth/components/LoadingScreen";
import { usePermissions } from "@shared/permissions/usePermissions";

type RequireRoutePermissionProps = {
  permissions: string[];
  mode?: "any" | "all";
  children: ReactElement;
};

export const RequireRoutePermission = ({
  permissions,
  mode = "any",
  children,
}: RequireRoutePermissionProps) => {
  const { isLoading, user } = useAuth();
  const { canAny, canAll } = usePermissions();
  const location = useLocation();

  if (isLoading) {
    return <LoadingScreen />;
  }

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  const allowed = mode === "all" ? canAll(permissions) : canAny(permissions);

  if (!allowed) {
    return <Navigate to="/forbidden" replace state={{ from: location }} />;
  }

  return children;
};
