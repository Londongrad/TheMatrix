// CitizensPage.tsx
import React, { useEffect, useState } from "react";
import CitizenCard from "../components/CitizenCard";
import CitizenDetailsModal from "../components/CitizenDetailsModal";
import type { PersonDto } from "../../../api/population/types";
import { getCitizensPage } from "../../../api/population/client";

const PAGE_SIZE = 100;

interface PaginationProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}

const Pagination: React.FC<PaginationProps> = ({
  page,
  totalPages,
  onChange,
}) => {
  const canGoPrev = page > 1;
  const canGoNext = page < totalPages;

  return (
    <div className="citizens-page-pagination">
      <button
        className="btn btn-sm"
        disabled={!canGoPrev}
        onClick={() => canGoPrev && onChange(1)}
      >
        First
      </button>

      <button
        className="btn btn-sm"
        disabled={!canGoPrev}
        onClick={() => canGoPrev && onChange(page - 1)}
      >
        Previous
      </button>

      <span className="pagination-info">
        Page {page} of {totalPages}
      </span>

      <button
        className="btn btn-sm"
        disabled={!canGoNext}
        onClick={() => canGoNext && onChange(page + 1)}
      >
        Next
      </button>

      <button
        className="btn btn-sm"
        disabled={!canGoNext}
        onClick={() => canGoNext && onChange(totalPages)}
      >
        Last
      </button>
    </div>
  );
};

const CitizensPage: React.FC = () => {
  const [citizens, setCitizens] = useState<PersonDto[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [selectedCitizenId, setSelectedCitizenId] = useState<string | null>(
    null
  );

  useEffect(() => {
    const fetchPage = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const result = await getCitizensPage(page, PAGE_SIZE);
        setCitizens(result.items);
        setTotal(result.totalCount);
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

  const startIndex = total === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const endIndex = total === 0 ? 0 : Math.min(page * PAGE_SIZE, total);

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

  // единственный "экшен" страницы — принять обновлённого PersonDto
  const handlePersonUpdated = (updated: PersonDto) => {
    setCitizens((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
  };

  return (
    <div className="citizens-page">
      <h1 className="page-title">Citizens</h1>

      {error && <p className="error-text">{error}</p>}

      <div className="citizens-page-toolbar">
        <div className="citizens-page-toolbar-left">
          {isLoading && <span className="card-sub">Loading citizens...</span>}

          {!isLoading && !error && total > 0 && (
            <span className="card-sub">
              Showing {startIndex}–{endIndex} of {total} citizens
            </span>
          )}
        </div>

        <Pagination page={page} totalPages={totalPages} onChange={setPage} />
      </div>

      {citizens.length > 0 && (
        <>
          <div className="cards-grid citizens-page-cards-grid">
            {citizens.map((person) => (
              <CitizenCard
                key={person.id}
                person={person}
                onEdit={openEditor} // только открыть модалку
              />
            ))}
          </div>

          <Pagination page={page} totalPages={totalPages} onChange={setPage} />
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
