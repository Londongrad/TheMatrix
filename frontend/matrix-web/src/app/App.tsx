import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layouts/MainLayout";
import DashboardPage from "@modules/city-core/pages/DashboardPage";
import CitizensPage from "@modules/population/pages/CitizensPage";
import UserSettingsPage from "@modules/identity/account/pages/user-settings/UserSettingsPage";

import { AuthProvider } from "@api/identity/auth/AuthContext";
import { RequireAuth } from "@api/identity/auth/RequireAuth";
import { LoginPage } from "@modules/identity/auth/pages/LoginPage";
import { RegisterPage } from "@modules/identity/auth/pages/RegisterPage";
import { ConfirmProvider } from "@modules/shared/components/ConfirmDialog";

const App = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ConfirmProvider>
          <Routes>
            {/* публичные страницы */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

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

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </ConfirmProvider>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
