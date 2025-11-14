import React, { useEffect, useState } from "react";
import CitizenCard from "../components/CitizenCard";
import type { PersonDto } from "../../../api/population/types";
import { getCitizensPage } from "../../../api/population/client";

const PAGE_SIZE = 20;

const CitizensPage: React.FC = () => {
  const [citizens, setCitizens] = useState<PersonDto[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchPage = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const result = await getCitizensPage(page, PAGE_SIZE);
        setCitizens(result.items);
        setTotal(result.total);
      } catch (e) {
        console.error(e);
        setError("Failed to load citizens.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchPage();
  }, [page]);

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="citizens-page">
      <h1 className="page-title">Citizens</h1>

      {error && <p className="error-text">{error}</p>}
      {isLoading && <p className="card-sub">Loading citizens...</p>}

      {!isLoading && !error && citizens.length === 0 && (
        <p className="card-sub">No citizens yet.</p>
      )}

      {citizens.length > 0 && (
        <>
          <div className="cards-grid citizens-grid">
            {citizens.map((person) => (
              <CitizenCard
                key={person.id}
                person={person}
                onKill={(id) => console.log("Kill", id)}
                onEdit={(id) => console.log("Edit", id)}
                onChangeHappiness={(id, delta) =>
                  console.log("Change happiness", id, delta)
                }
              />
            ))}
          </div>

          <div className="pagination">
            <button
              className="btn btn-sm"
              disabled={page === 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </button>

            <span className="pagination-info">
              Page {page} of {totalPages}
            </span>

            <button
              className="btn btn-sm"
              disabled={page === totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </>
      )}
    </div>
  );
};

export default CitizensPage;
