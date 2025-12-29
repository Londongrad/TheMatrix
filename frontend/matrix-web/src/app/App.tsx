import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";

import CitizensPage from "@services/population/pages/CitizensPage";
import DashboardPage from "@services/citycore/pages/DashboardPage";
import ForbiddenPage from "@pages/forbidden-page/ForbiddenPage";

import AdminUsersPage from "@services/identity/admin/pages/users/AdminUsersPage";
import AdminRolesPage from "@services/identity/admin/pages/roles/AdminRolesPage";
import AdminPermissionsPage from "@services/identity/admin/pages/permissions/AdminPermissionsPage";

import UserSettingsPage from "@services/identity/self/account/pages/user-settings/pages/UserSettingsPage";
import UserSettingsSecurityPage from "@services/identity/self/account/pages/user-settings/pages/UserSettingsSecurityPage";
import UserSettingsSessionsPage from "@services/identity/self/account/pages/user-settings/pages/UserSettingsSessionsPage";
import UserSettingsPreferencesPage from "@services/identity/self/account/pages/user-settings/pages/UserSettingsPreferencesPage";
import UserSettingsDangerPage from "@services/identity/self/account/pages/user-settings/pages/UserSettingsDangerPage";

import { AuthProvider } from "@services/identity/api/self/auth/AuthContext";
import { RequireAuth } from "@services/identity/api/self/auth/RequireAuth";
import { LoginPage } from "@services/identity/self/auth/pages/LoginPage";
import { RegisterPage } from "@services/identity/self/auth/pages/RegisterPage";
import { ForgotPasswordPage } from "@services/identity/self/auth/pages/ForgotPasswordPage";
import { ConfirmProvider } from "@shared/ui/ConfirmDialog/ConfirmDialog";

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
              <Route path="/citizens" element={<CitizensPage />} />
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
              <Route path="profile" element={<UserSettingsPage />} />
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
                  <AdminLayout />
                </RequireAuth>
              }
            >
              <Route index element={<Navigate to="users" replace />} />
              <Route path="users" element={<AdminUsersPage />} />
              <Route path="roles" element={<AdminRolesPage />} />
              <Route path="permissions" element={<AdminPermissionsPage />} />
            </Route>
          </Routes>
        </ConfirmProvider>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
