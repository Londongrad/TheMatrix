// src/pages/forbidden-page/ForbiddenPage.tsx
import { useLocation, useNavigate } from "react-router-dom";
import { useMemo } from "react";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import MatrixBackdrop from "@shared/ui/backgrounds/BackgroundRain/MatrixRainBackground";
import "./forbidden-page.css";

type ForbiddenState = {
  from?: string;
  url?: string;
  reason?: string;
};

type RainColumn = {
  id: number;
  leftPct: number;
  delaySec: number;
  durationSec: number;
  text: string;
};

function randomInt(min: number, max: number) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function pick<T>(arr: T[]) {
  return arr[Math.floor(Math.random() * arr.length)];
}

function makeColumnText(length: number) {
  const glyphs = [
    ..."01",
    ..."1010011010",
    ..."アイウエオカキクケコサシスセソ",
    ..."ﾊﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝ",
    ..."∆⌁⌂⌐⌑⌒⍟⎔⎓⎖⎗",
  ].flat();

  // Столбик “символ на строку” (white-space: pre)
  let s = "";
  for (let i = 0; i < length; i++) {
    s += pick(glyphs) + (i === length - 1 ? "" : "\n");
  }
  return s;
}

export default function ForbiddenPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { logout } = useAuth();

  const state = (location.state ?? {}) as ForbiddenState;

  const rain = useMemo<RainColumn[]>(() => {
    const columns = 34;
    const res: RainColumn[] = [];

    for (let i = 0; i < columns; i++) {
      const leftPct = (i / columns) * 100 + Math.random() * 2; // небольшой “дрейф”
      const delaySec = -Math.random() * 12; // чтобы анимация стартовала “в процессе”
      const durationSec = randomInt(6, 14) + Math.random();
      const textLen = randomInt(18, 38);

      res.push({
        id: i,
        leftPct,
        delaySec,
        durationSec,
        text: makeColumnText(textLen),
      });
    }

    return res;
  }, []);

  const handleLogout = async () => {
    try {
      await logout();
    } finally {
      navigate("/login", { replace: true });
    }
  };

  return (
    <div className="forbidden">
      <MatrixBackdrop rainOpacity={0.3} />

      {/* контент */}
      <div className="forbidden__wrap">
        <div className="forbidden__card" role="region" aria-label="Нет доступа">
          <div className="forbidden__badge">
            <span className="forbidden__dot" />
            Security Layer
          </div>

          <h1 className="forbidden__title">
            <span className="forbidden__glitch" data-text="403">
              403
            </span>
            <span className="forbidden__titleSub">ACCESS DENIED</span>
          </h1>

          <p className="forbidden__text">
            У вашей учётной записи недостаточно прав для выполнения этого
            действия.
          </p>

          {state.reason ? (
            <p className="forbidden__text forbidden__text--muted">
              Причина: <span className="forbidden__mono">{state.reason}</span>
            </p>
          ) : null}

          {(state.from || state.url) && (
            <details className="forbidden__details">
              <summary>Детали запроса</summary>
              <div className="forbidden__code">
                {state.from ? (
                  <div className="forbidden__codeRow">
                    <span className="forbidden__codeKey">from</span>
                    <span className="forbidden__codeSep">=</span>
                    <code className="forbidden__codeVal">{state.from}</code>
                  </div>
                ) : null}

                {state.url ? (
                  <div className="forbidden__codeRow">
                    <span className="forbidden__codeKey">url</span>
                    <span className="forbidden__codeSep">=</span>
                    <code className="forbidden__codeVal">{state.url}</code>
                  </div>
                ) : null}
              </div>
            </details>
          )}

          <div className="forbidden__actions">
            <button
              className="forbidden__btn"
              type="button"
              onClick={() => navigate(-1)}
            >
              Назад
            </button>

            <button
              className="forbidden__btn forbidden__btn--primary"
              type="button"
              onClick={() => navigate("/", { replace: true })}
            >
              На главную
            </button>

            <button
              className="forbidden__btn forbidden__btn--danger"
              type="button"
              onClick={handleLogout}
            >
              Выйти
            </button>
          </div>

          <div className="forbidden__hint">
            <span className="forbidden__hintLabel">Tip:</span> если доступ
            должен быть — проверь роли/claims и policies в Gateway/Identity.
          </div>
        </div>
      </div>
    </div>
  );
}
