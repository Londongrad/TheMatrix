import React from "react";

const DashboardPage: React.FC = () => {
  const handleTriggerStorm = () => {
    console.log("Trigger storm in district #1");
    // TODO: вызов backend, когда появится API
  };

  const handleTriggerBlackout = () => {
    console.log("Trigger blackout for 15 minutes");
  };

  return (
    <div className="dashboard">
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
    </div>
  );
};

export default DashboardPage;
