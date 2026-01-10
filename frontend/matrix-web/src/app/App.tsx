import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";

import CitizensPage from "@services/population/people/pages/CitizensPage";
import DashboardPage from "@services/citycore/pages/DashboardPage";
import ForbiddenPage from "@pages/forbidden-page/ForbiddenPage";

import AdminUsersPage from "@services/identity/admin/users/pages/AdminUsersPage";
import AdminRolesPage from "@services/identity/admin/roles/pages/AdminRolesPage";
import AdminPermissionsPage from "@services/identity/admin/permissions/pages/AdminPermissionsPage";

import UserSettingsProfilePage from "@services/identity/self/account/profile/pages/UserSettingsProfilePage";
import UserSettingsSecurityPage from "@services/identity/self/account/security/pages/UserSettingsSecurityPage";
import UserSettingsSessionsPage from "@services/identity/self/account/sessions/pages/UserSettingsSessionsPage";
import UserSettingsPreferencesPage from "@services/identity/self/account/preferences/pages/UserSettingsPreferencesPage";
import UserSettingsDangerPage from "@services/identity/self/account/danger/pages/UserSettingsDangerPage";

import { AuthProvider } from "@services/identity/api/self/auth/AuthContext";
import { RequireAuth } from "@services/identity/api/self/auth/RequireAuth";
import { LoginPage } from "@services/identity/self/auth/pages/LoginPage";
import { RegisterPage } from "@services/identity/self/auth/pages/RegisterPage";
import { ForgotPasswordPage } from "@services/identity/self/auth/pages/ForgotPasswordPage";
import { ConfirmProvider } from "@shared/ui/components/ConfirmDialog/ConfirmDialog";
import { RequireRoutePermission } from "@app/router/guards/RequireRoutePermission";
import { PermissionKeys } from "@shared/permissions/permissionKeys";

import MainLayout from "./layouts/main/MainLayout";
import AdminLayout from "./layouts/admin/AdminLayout";
import UserSettingsLayout from "./layouts/user-settings/UserSettingsLayout";

const App = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ConfirmProvider>
          <Routes>
            {/* публичные страницы */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/forbidden" element={<ForbiddenPage />} />

            {/* защищённые страницы — с MainLayout */}
            <Route
              element={
                <RequireAuth>
                  <MainLayout />
                </RequireAuth>
              }
            >
              <Route path="/" element={<DashboardPage />} />
              <Route
                path="/citizens"
                element={
                  <RequireRoutePermission
                    permissions={[PermissionKeys.PopulationPeopleRead]}
                  >
                    <CitizensPage />
                  </RequireRoutePermission>
                }
              />
            </Route>

            {/* защищённые user settings страницы - с UserSettingsLayout */}
            <Route
              path="/userSettings"
              element={
                <RequireAuth>
                  <UserSettingsLayout />
                </RequireAuth>
              }
            >
              <Route index element={<Navigate to="profile" replace />} />
              <Route path="profile" element={<UserSettingsProfilePage />} />
              <Route path="security" element={<UserSettingsSecurityPage />} />
              <Route path="sessions" element={<UserSettingsSessionsPage />} />
              <Route
                path="preferences"
                element={<UserSettingsPreferencesPage />}
              />
              <Route path="danger" element={<UserSettingsDangerPage />} />
            </Route>

            {/* защищённые admin страницы - с AdminLayout */}
            <Route
              path="/admin"
              element={
                <RequireAuth>
                  <RequireRoutePermission
                    permissions={[PermissionKeys.IdentityAdminAccess]}
                  >
                    <AdminLayout />
                  </RequireRoutePermission>
                </RequireAuth>
              }
            >
              <Route index element={<Navigate to="users" replace />} />
              <Route
                path="users"
                element={
                  <RequireRoutePermission
                    permissions={[PermissionKeys.IdentityUsersRead]}
                  >
                    <AdminUsersPage />
                  </RequireRoutePermission>
                }
              />
              <Route
                path="roles"
                element={
                  <RequireRoutePermission
                    permissions={[PermissionKeys.IdentityRolesList]}
                  >
                    <AdminRolesPage />
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
                    <AdminPermissionsPage />
                  </RequireRoutePermission>
                }
              />
            </Route>
          </Routes>
        </ConfirmProvider>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
