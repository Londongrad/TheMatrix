import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layouts/MainLayout";
import DashboardPage from "../modules/city-core/pages/DashboardPage";
import CitizensPage from "../modules/population/pages/CitizensPage";

import { AuthProvider } from "../api/auth/AuthContext";
import { RequireAuth } from "../api/auth/RequireAuth";
import { LoginPage } from "../modules/auth/pages/LoginPage";
import { RegisterPage } from "../modules/auth/pages/RegisterPage";

const App = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
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
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
