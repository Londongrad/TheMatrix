import "@services/identity/auth/styles/loading-screen.css";

export const LoadingScreen = () => {
  return (
    <div className="loading-overlay">
      <div className="loading-core">
        <div className="loading-ring">
          <div className="loading-orb" />
        </div>
        <div className="loading-title">Подключаемся к Матрице…</div>
        <div className="loading-subtitle">Загружаем сессию администратора</div>
      </div>
    </div>
  );
};
