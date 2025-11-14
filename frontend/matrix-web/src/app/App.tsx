import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import MainLayout from "./layouts/MainLayout";
import DashboardPage from "../modules/city-core/pages/DashboardPage";
import CitizensPage from "../modules/population/pages/CitizensPage";

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <MainLayout>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/citizens" element={<CitizensPage />} />

          {/* Заглушки на будущее */}
          {/* <Route path="/incidents" element={<IncidentsPage />} /> */}
          {/* <Route path="/systems" element={<SystemsPage />} /> */}
          {/* <Route path="/settings" element={<SettingsPage />} /> */}

          {/* всё неизвестное ведём на дашборд */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </MainLayout>
    </BrowserRouter>
  );
};

export default App;
