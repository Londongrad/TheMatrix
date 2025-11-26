import "../../../styles/population/citizen-page.css";
import { useEffect, useState } from "react";
import CitizenCard from "../components/CitizenCard";
import CitizenDetailsModal from "../components/CitizenDetailsModal";
import type { PersonDto } from "../../../api/population/populationTypes";
import { getCitizensPage } from "../../../api/population/populationApi";
import { useAuth } from "../../../api/auth/AuthContext";
import Pagination from "../components/Pagination";

const PAGE_SIZE = 100;

const CitizensPage = () => {
  const [citizens, setCitizens] = useState<PersonDto[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalCitizens, setTotalCitizens] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Access token
  const { token } = useAuth();

  const [selectedCitizenId, setSelectedCitizenId] = useState<string | null>(
    null
  );

  useEffect(() => {
    // 1. Если токена нет — не делаем запрос
    if (!token) {
      return;
    }

    // 2. Фиксируем токен в локальной переменной, чтобы TS понял, что он уже точно string
    const accessToken = token;

    const fetchPage = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const result = await getCitizensPage(
          pageNumber,
          PAGE_SIZE,
          accessToken
        );
        setCitizens(result.items);
        setTotalCitizens(result.totalCount);
      } catch (e) {
        console.error(e);
        setError("Failed to load citizens.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchPage();
  }, [pageNumber, token]);

  const totalPages = Math.max(1, Math.ceil(totalCitizens / PAGE_SIZE));

  const startIndex = totalCitizens === 0 ? 0 : (pageNumber - 1) * PAGE_SIZE + 1;
  const endIndex =
    totalCitizens === 0 ? 0 : Math.min(pageNumber * PAGE_SIZE, totalCitizens);

  const selectedCitizen =
    selectedCitizenId != null
      ? citizens.find((c) => c.id === selectedCitizenId) ?? null
      : null;

  const openEditor = (id: string) => {
    setSelectedCitizenId(id);
  };

  const closeEditor = () => {
    setSelectedCitizenId(null);
  };

  const handlePersonUpdated = (updated: PersonDto) => {
    setCitizens((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
  };

  return (
    <div className="citizens-page">
      <h1 className="page-title">Citizens</h1>

      <div className="citizens-page-toolbar">
        <div className="citizens-page-toolbar-left">
          {isLoading && (
            <div className="citizens-loading-inline">
              <span className="citizens-loading-spinner" />
              <span className="card-sub">Loading citizens…</span>
            </div>
          )}

          {error && <p className="error-text">{error}</p>}

          {!isLoading && !error && totalCitizens > 0 && (
            <span className="card-sub">
              Showing {startIndex}–{endIndex} of {totalCitizens} citizens
            </span>
          )}
        </div>

        <Pagination
          page={pageNumber}
          totalPages={totalPages}
          onChange={setPageNumber}
        />
      </div>

      {citizens.length > 0 && (
        <>
          <div className="citizens-page-cards-grid">
            {citizens.map((person) => (
              <CitizenCard
                key={person.id}
                person={person}
                onEdit={openEditor}
              />
            ))}
          </div>

          <Pagination
            page={pageNumber}
            totalPages={totalPages}
            onChange={setPageNumber}
          />
        </>
      )}

      {!isLoading && !error && citizens.length === 0 && (
        <p className="card-sub">No citizens yet.</p>
      )}

      <CitizenDetailsModal
        person={selectedCitizen}
        isOpen={selectedCitizen != null}
        onClose={closeEditor}
        onPersonUpdated={handlePersonUpdated}
      />
    </div>
  );
};

export default CitizensPage;
