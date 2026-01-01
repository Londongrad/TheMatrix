import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@services/identity/api/auth/AuthContext";

type ForbiddenState = {
  from?: string;
  url?: string;
};

export default function ForbiddenPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout } = useAuth();

  const state = (location.state ?? {}) as ForbiddenState;

  return (
    <div style={{ padding: 24 }}>
      <h1>Нет доступа</h1>
      <p>
        У вашей учётной записи недостаточно прав для выполнения этого действия.
      </p>

      {state.from ? (
        <p style={{ opacity: 0.75 }}>
          Откуда: <code>{state.from}</code>
        </p>
      ) : null}

      <div style={{ display: "flex", gap: 12, marginTop: 16 }}>
        <button onClick={() => navigate(-1)}>Назад</button>
        <button onClick={() => navigate("/", { replace: true })}>
          На главную
        </button>
        <button onClick={() => logout()}>Выйти</button>
      </div>
    </div>
  );
}
