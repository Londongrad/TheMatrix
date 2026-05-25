import {Navigate, useLocation} from "react-router";
import type {ReactElement} from "react";
import {useAuth} from "@services/identity/api/self/auth/useAuth";
import {LoadingScreen} from "@services/identity/self/auth/components/LoadingScreen";
import {usePermissions} from "@shared/permissions/usePermissions";
import type {PermissionMatchMode} from "@shared/permissions/permissionMatchMode";

type RequireRoutePermissionProps = {
    permissions: string[];
    permissionMatchMode?: PermissionMatchMode;
    children: ReactElement;
};

export const RequireRoutePermission = ({
                                           permissions,
                                           permissionMatchMode = "any",
                                           children,
                                       }: RequireRoutePermissionProps) => {
    const {isLoading, user} = useAuth();
    const {canAny, canAll} = usePermissions();
    const location = useLocation();

    if (isLoading) {
        return <LoadingScreen/>;
    }

    if (!user) {
        return <Navigate to="/login" state={{from: location}} replace/>;
    }

    const allowed = permissionMatchMode === "all"
        ? canAll(permissions)
        : canAny(permissions);

    if (!allowed) {
        return <Navigate to="/forbidden" replace state={{from: location}}/>;
    }

    return children;
};
