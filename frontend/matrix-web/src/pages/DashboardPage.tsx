import React, { useState } from "react";
import { getPopulationPreview } from "../api/population/client";
import type { PersonDto } from "../api/population/types";
import CitizenCard from "../components/population/CitizenCard";

const DashboardPage: React.FC = () => {
  const [citizens, setCitizens] = useState<PersonDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleTriggerStorm = () => {
    console.log("Trigger storm in district #1");
    // TODO: вызов backend, когда появится API
  };

  const handleTriggerBlackout = () => {
    console.log("Trigger blackout for 15 minutes");
  };

  const handleGenerateCitizensPreview = async () => {
    try {
      setIsLoading(true);
      setError(null);

      const data = await getPopulationPreview(100);
      setCitizens(data);
    } catch (e) {
      console.error(e);
      setError("Failed to load population preview.");
    } finally {
      setIsLoading(false);
    }
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

      <section className="population-preview">
        <h2 className="section-title">Population preview</h2>
        <div className="actions-row">
          <button
            className="btn btn-primary"
            onClick={handleGenerateCitizensPreview}
            disabled={isLoading}
          >
            {isLoading ? "Generating..." : "Generate 100 citizens"}
          </button>
        </div>

        {error && <p className="error-text">{error}</p>}

        {citizens.length > 0 && (
          <div className="cards-grid citizens-grid">
            {citizens.map((person) => (
              <CitizenCard key={person.id} person={person} />
            ))}
          </div>
        )}

        {!isLoading && !error && citizens.length === 0 && (
          <p className="card-sub">No preview generated yet.</p>
        )}
      </section>
    </div>
  );
};

export default DashboardPage;
