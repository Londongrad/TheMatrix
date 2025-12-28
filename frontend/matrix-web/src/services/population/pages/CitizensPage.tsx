// src/services/population/pages/CitizenPage.tsx
import { useEffect, useState } from "react";
import CitizenCard from "@services/population/components/CitizenCard";
import CitizenDetailsModal from "@services/population/components/CitizenDetailsModal";
import type { PersonDto } from "@services/population/api/populationTypes";
import { getCitizensPage } from "@services/population/api/populationApi";
import { useAuth } from "@services/identity/api/auth/AuthContext";
import Pagination from "@services/population/components/Pagination";
import "@services/population/styles/citizen-page.css";

const PAGE_SIZE = 100;

const CitizensPage = () => {
  const [citizens, setCitizens] = useState<PersonDto[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalCitizens, setTotalCitizens] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { token } = useAuth();

  const [selectedCitizenId, setSelectedCitizenId] = useState<string | null>(
    null
  );

  useEffect(() => {
    // (1) Нет токена — чистим состояние, чтобы не висели старые данные
    if (!token) {
      setCitizens([]);
      setTotalCitizens(0);
      setPageNumber(1);
      setSelectedCitizenId(null);
      setError(null);
      setIsLoading(false);
      return;
    }

    const accessToken = token;

    // (2) Защита от гонок: игнорируем поздние ответы
    let isActual = true;

    (async () => {
      try {
        setIsLoading(true);
        setError(null);

        const result = await getCitizensPage(
          pageNumber,
          PAGE_SIZE,
          accessToken
        );

        if (!isActual) return;
        setCitizens(result.items);
        setTotalCitizens(result.totalCount);
      } catch (e) {
        if (!isActual) return;
        console.error(e);
        setError("Failed to load citizens.");
        // UX: оставляем прошлые данные на экране (не очищаем citizens)
      } finally {
        if (!isActual) return;
        setIsLoading(false);
      }
    })();

    return () => {
      isActual = false;
    };
  }, [pageNumber, token]);

  const totalPages = Math.max(1, Math.ceil(totalCitizens / PAGE_SIZE));

  const startIndex = totalCitizens === 0 ? 0 : (pageNumber - 1) * PAGE_SIZE + 1;
  const endIndex =
    totalCitizens === 0 ? 0 : Math.min(pageNumber * PAGE_SIZE, totalCitizens);

  const selectedCitizen =
    selectedCitizenId != null
      ? citizens.find((c) => c.id === selectedCitizenId) ?? null
      : null;

  const openEditor = (id: string) => setSelectedCitizenId(id);
  const closeEditor = () => setSelectedCitizenId(null);

  const handlePersonUpdated = (updated: PersonDto) => {
    setCitizens((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
  };

  const paginationDisabled = isLoading || !token;

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
          disabled={paginationDisabled}
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
            disabled={paginationDisabled}
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
