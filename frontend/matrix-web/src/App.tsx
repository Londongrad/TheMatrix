import React from "react";
import MainLayout from "./layouts/MainLayout";
import DashboardPage from "./pages/DashboardPage";

const App: React.FC = () => {
  return (
    <MainLayout>
      <DashboardPage />
    </MainLayout>
  );
};

export default App;
