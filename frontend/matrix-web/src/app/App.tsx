import {lazy, Suspense} from "react";
import {BrowserRouter, Navigate, Route, Routes} from "react-router";

import {AuthProvider} from "@services/identity/api/self/auth/AuthContext";
import {RequireAuth} from "@services/identity/api/self/auth/RequireAuth";
import {LoadingScreen} from "@services/identity/self/auth/components/LoadingScreen";
import {ConfirmProvider} from "@shared/ui/components/ConfirmDialog/ConfirmDialog";
import {RequireRoutePermission} from "@app/router/guards/RequireRoutePermission";
import {classicCityRoutes, simulationCoreCatalogRoutes} from "@app/router/SimulationCoreRoutes";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {RouteErrorBoundary} from "@app/errors/RouteErrorBoundary";

import MainLayout from "./layouts/main/MainLayout";
import AdminLayout from "./layouts/admin/AdminLayout";
import UserSettingsLayout from "./layouts/user-settings/UserSettingsLayout";
import ClassicCityLayout from "./layouts/classic-city/ClassicCityLayout";

const DashboardPage = lazy(() => import("@services/simulationcore/dashboard/pages/DashboardPage"));
const ForbiddenPage = lazy(() => import("@pages/forbidden-page/ForbiddenPage"));
const NotFoundPage = lazy(() => import("@pages/not-found-page/NotFoundPage"));

const AdminUsersPage = lazy(() => import("@services/identity/admin/users/pages/AdminUsersPage"));
const AdminRolesPage = lazy(() => import("@services/identity/admin/roles/pages/AdminRolesPage"));
const AdminPermissionsPage = lazy(
    () => import("@services/identity/admin/permissions/pages/AdminPermissionsPage"),
);

const UserSettingsAccountPage = lazy(
    () => import("@services/identity/self/account/account/pages/UserSettingsAccountPage"),
);
const UserSettingsPersonalizationPage = lazy(
    () => import("@services/identity/self/account/personalization/pages/UserSettingsPersonalizationPage"),
);
const UserSettingsSecurityPage = lazy(
    () => import("@services/identity/self/account/security/pages/UserSettingsSecurityPage"),
);
const UserSettingsSessionsPage = lazy(
    () => import("@services/identity/self/account/sessions/pages/UserSettingsSessionsPage"),
);
const UserSettingsWorkspacePage = lazy(
    () => import("@services/identity/self/account/workspace/pages/UserSettingsWorkspacePage"),
);
const UserSettingsDangerPage = lazy(
    () => import("@services/identity/self/account/danger/pages/UserSettingsDangerPage"),
);

const LoginPage = lazy(() =>
    import("@services/identity/self/auth/pages/LoginPage").then((module) => ({
        default: module.LoginPage,
    })),
);
const RegisterPage = lazy(() =>
    import("@services/identity/self/auth/pages/RegisterPage").then((module) => ({
        default: module.RegisterPage,
    })),
);
const ConfirmEmailPage = lazy(() =>
    import("@services/identity/self/auth/pages/ConfirmEmailPage").then((module) => ({
        default: module.ConfirmEmailPage,
    })),
);
const ConfirmEmailChangePage = lazy(() =>
    import("@services/identity/self/auth/pages/ConfirmEmailChangePage").then((module) => ({
        default: module.ConfirmEmailChangePage,
    })),
);
const ConfirmAccountRecoveryPage = lazy(() =>
    import("@services/identity/self/auth/pages/ConfirmAccountRecoveryPage").then((module) => ({
        default: module.ConfirmAccountRecoveryPage,
    })),
);
const ForgotPasswordPage = lazy(() =>
    import("@services/identity/self/auth/pages/ForgotPasswordPage").then((module) => ({
        default: module.ForgotPasswordPage,
    })),
);
const ResetPasswordPage = lazy(() =>
    import("@services/identity/self/auth/pages/ResetPasswordPage").then((module) => ({
        default: module.ResetPasswordPage,
    })),
);
const RecoverAccountPage = lazy(() =>
    import("@services/identity/self/auth/pages/RecoverAccountPage").then((module) => ({
        default: module.RecoverAccountPage,
    })),
);

const App = () => {
    return (
        <BrowserRouter>
            <AuthProvider>
                <ConfirmProvider>
                    <RouteErrorBoundary>
                        <Suspense fallback={<LoadingScreen/>}>
                            <Routes>
                                {/* Public pages */}
                                <Route path="/login" element={<LoginPage/>}/>
                                <Route path="/register" element={<RegisterPage/>}/>
                                <Route path="/confirm-email" element={<ConfirmEmailPage/>}/>
                                <Route path="/confirm-email-change" element={<ConfirmEmailChangePage/>}/>
                                <Route path="/forgot-password" element={<ForgotPasswordPage/>}/>
                                <Route path="/recover-account" element={<RecoverAccountPage/>}/>
                                <Route path="/confirm-account-recovery" element={<ConfirmAccountRecoveryPage/>}/>
                                <Route path="/reset-password" element={<ResetPasswordPage/>}/>
                                <Route path="/forbidden" element={<ForbiddenPage/>}/>

                                {/* Protected pages with MainLayout */}
                                <Route
                                    element={
                                        <RequireAuth>
                                            <MainLayout/>
                                        </RequireAuth>
                                    }
                                >
                                    <Route path="/" element={<DashboardPage/>}/>
                                    {simulationCoreCatalogRoutes}
                                </Route>

                                <Route
                                    element={
                                        <RequireAuth>
                                            <ClassicCityLayout/>
                                        </RequireAuth>
                                    }
                                >
                                    {classicCityRoutes}
                                </Route>

                                {/* Protected user settings pages with UserSettingsLayout */}
                                <Route
                                    path="/userSettings"
                                    element={
                                        <RequireAuth>
                                            <UserSettingsLayout/>
                                        </RequireAuth>
                                    }
                                >
                                    <Route index element={<Navigate to="account" replace/>}/>
                                    <Route path="profile" element={<Navigate to="/userSettings/account" replace/>}/>
                                    <Route
                                        path="preferences"
                                        element={<Navigate to="/userSettings/workspace" replace/>}
                                    />
                                    <Route path="account" element={<UserSettingsAccountPage/>}/>
                                    <Route
                                        path="personalization"
                                        element={<UserSettingsPersonalizationPage/>}
                                    />
                                    <Route path="security" element={<UserSettingsSecurityPage/>}/>
                                    <Route path="sessions" element={<UserSettingsSessionsPage/>}/>
                                    <Route path="workspace" element={<UserSettingsWorkspacePage/>}/>
                                    <Route path="danger" element={<UserSettingsDangerPage/>}/>
                                </Route>

                                {/* Protected admin pages with AdminLayout */}
                                <Route
                                    path="/admin"
                                    element={
                                        <RequireAuth>
                                            <RequireRoutePermission
                                                permissions={[
                                                    PermissionKeys.IdentityUsersRead,
                                                    PermissionKeys.IdentityRolesList,
                                                    PermissionKeys.IdentityPermissionsCatalogRead,
                                                ]}
                                                permissionMatchMode="any"
                                            >
                                                <AdminLayout/>
                                            </RequireRoutePermission>
                                        </RequireAuth>
                                    }
                                >
                                    <Route index element={<Navigate to="users" replace/>}/>
                                    <Route
                                        path="users"
                                        element={
                                            <RequireRoutePermission
                                                permissions={[PermissionKeys.IdentityUsersRead]}
                                            >
                                                <AdminUsersPage/>
                                            </RequireRoutePermission>
                                        }
                                    />
                                    <Route
                                        path="roles"
                                        element={
                                            <RequireRoutePermission
                                                permissions={[PermissionKeys.IdentityRolesList]}
                                            >
                                                <AdminRolesPage/>
                                            </RequireRoutePermission>
                                        }
                                    />
                                    <Route
                                        path="permissions"
                                        element={
                                            <RequireRoutePermission
                                                permissions={[
                                                    PermissionKeys.IdentityPermissionsCatalogRead,
                                                ]}
                                            >
                                                <AdminPermissionsPage/>
                                            </RequireRoutePermission>
                                        }
                                    />
                                </Route>

                                <Route path="*" element={<NotFoundPage/>}/>
                            </Routes>
                        </Suspense>
                    </RouteErrorBoundary>
                </ConfirmProvider>
            </AuthProvider>
        </BrowserRouter>
    );
};

export default App;
