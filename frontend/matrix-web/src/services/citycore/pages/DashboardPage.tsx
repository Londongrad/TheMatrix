import { useState } from "react";
import { initializePopulation } from "@services/population/api/populationApi";
import { useAuth } from "@services/identity/api/auth/AuthContext";
import "@services/citycore/styles/dashboard.css";

const DashboardPage = () => {
  const [generateCount, setGenerateCount] = useState(10000);
  const [isInitializing, setIsInitializing] = useState(false);
  const [initError, setInitError] = useState<string | null>(null);
  const [initMessage, setInitMessage] = useState<string | null>(null);

  const { token } = useAuth(); // берем access токен

  const handleTriggerStorm = () => {
    console.log("Trigger storm in district #1");
    // TODO: вызов backend, когда появится API
  };

  const handleTriggerBlackout = () => {
    console.log("Trigger blackout for 15 minutes");
  };

  const handleInitializePopulation = async () => {
    // простая валидация
    if (!Number.isFinite(generateCount) || generateCount <= 0) {
      setInitError("Please enter a positive number of citizens.");
      return;
    }

    if (!token) {
      setInitError("Not authenticated.");
      return;
    }

    try {
      setIsInitializing(true);
      setInitError(null);
      setInitMessage(null);

      await initializePopulation(generateCount, token);

      setInitMessage(
        `Population initialized with ${generateCount.toLocaleString()} citizens.`
      );
    } catch (e) {
      console.error(e);
      setInitError("Failed to initialize population.");
    } finally {
      setIsInitializing(false);
    }
  };

  return (
    <>
      <h1 className="page-title">Dashboard</h1>

      <section className="cards-grid">
        <div className="card">
          <h2 className="card-title">Population</h2>
          <p className="card-value">1 234 567</p>
          <p className="card-sub">citizens in simulation</p>
        </div>

        <div className="card">
          <h2 className="card-title">Active incidents</h2>
          <p className="card-value">12</p>
          <p className="card-sub">need operator attention</p>
        </div>

        <div className="card">
          <h2 className="card-title">System status</h2>
          <p className="card-value">Stable</p>
          <p className="card-sub">no critical failures</p>
        </div>
      </section>

      <section className="actions">
        <h2 className="section-title">God actions</h2>
        <div className="actions-row">
          <button className="btn btn-primary" onClick={handleTriggerStorm}>
            Trigger thunderstorm
          </button>
          <button className="btn btn-danger" onClick={handleTriggerBlackout}>
            Trigger blackout
          </button>
          <button className="btn" disabled>
            Spawn random event (soon)
          </button>
        </div>
      </section>

      {/* секция генерации населения */}
      <section className="actions" style={{ marginTop: "24px" }}>
        <h2 className="section-title">Population initialization</h2>
        <div className="actions-row">
          <input
            type="number"
            min={1}
            className="text-input"
            value={generateCount}
            onChange={(e) => setGenerateCount(Number(e.target.value))}
          />
          <button
            className="btn btn-primary"
            onClick={handleInitializePopulation}
            disabled={isInitializing}
          >
            {isInitializing ? "Initializing..." : "Generate citizens"}
          </button>
        </div>

        {initError && <p className="error-text">{initError}</p>}
        {initMessage && <p className="card-sub">{initMessage}</p>}
      </section>
    </>
  );
};

export default DashboardPage;
