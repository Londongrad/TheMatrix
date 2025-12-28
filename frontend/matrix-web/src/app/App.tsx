import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layouts/MainLayout";
import DashboardPage from "@services/citycore/pages/DashboardPage";
import CitizensPage from "@services/population/pages/CitizensPage";
import UserSettingsPage from "@services/identity/account/pages/user-settings/UserSettingsPage";
import AdminUsersPage from "@services/identity/admin/pages/AdminUsersPage";

import { AuthProvider } from "@services/identity/api/self/auth/AuthContext";
import { RequireAuth } from "@services/identity/api/self/auth/RequireAuth";
import { LoginPage } from "@services/identity/auth/pages/LoginPage";
import { RegisterPage } from "@services/identity/auth/pages/RegisterPage";
import { ConfirmProvider } from "@shared/components/ConfirmDialog";
import ForbiddenPage from "@shared/pages/ForbiddenPage";

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

            {/* защищённые страницы — только для залогиненных */}
            <Route
              path="/"
              element={
                <RequireAuth>
                  <MainLayout>
                    <DashboardPage />
                  </MainLayout>
                </RequireAuth>
              }
            />

            <Route
              path="/userSettings"
              element={
                <RequireAuth>
                  <MainLayout>
                    <UserSettingsPage />
                  </MainLayout>
                </RequireAuth>
              }
            />

            <Route
              path="/citizens"
              element={
                <RequireAuth>
                  <MainLayout>
                    <CitizensPage />
                  </MainLayout>
                </RequireAuth>
              }
            />

            <Route
              path="/admin/users"
              element={
                <RequireAuth>
                  <MainLayout>
                    <AdminUsersPage />
                  </MainLayout>
                </RequireAuth>
              }
            />

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </ConfirmProvider>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
