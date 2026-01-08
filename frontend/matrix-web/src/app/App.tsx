import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layouts/main/MainLayout";
import DashboardPage from "@services/citycore/pages/DashboardPage";
import CitizensPage from "@services/population/pages/CitizensPage";
import UserSettingsPage from "@services/identity/self/account/pages/user-settings/UserSettingsPage";
import AdminUsersPage from "@services/identity/admin/pages/users/AdminUsersPage";
import AdminRolesPage from "@services/identity/admin/pages/roles/AdminRolesPage";
import AdminPermissionsPage from "@services/identity/admin/pages/permissions/AdminPermissionsPage";

import { AuthProvider } from "@services/identity/api/self/auth/AuthContext";
import { RequireAuth } from "@services/identity/api/self/auth/RequireAuth";
import { LoginPage } from "@services/identity/self/auth/pages/LoginPage";
import { RegisterPage } from "@services/identity/self/auth/pages/RegisterPage";
import { ConfirmProvider } from "@shared/ui/ConfirmDialog/ConfirmDialog";
import ForbiddenPage from "@pages/forbidden-page/ForbiddenPage";
import AdminLayout from "./layouts/admin/AdminLayout";

const App = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ConfirmProvider>
          <Routes>
            {/* публичные страницы */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
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
              <Route path="/userSettings" element={<UserSettingsPage />} />
              <Route path="/citizens" element={<CitizensPage />} />
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
