// src/services/population/pages/CitizensPage.tsx
import { useState } from "react";
import CitizenCard from "@services/population/person/components/CitizenCard";
import CitizenDetailsModal from "@services/population/person/components/CitizenDetailsModal";
import type { PersonDto } from "@services/population/person/api/personTypes";
import { getCitizensPage } from "@services/population/people/api/peopleApi";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import { usePagedQuery } from "@shared/lib/paging/usePagedQuery";
import { getPageRange } from "@shared/lib/paging/pageRange";
import "@services/population/people/styles/citizen-page.css";

const PAGE_SIZE = 100;

const CitizensPage = () => {
  const { token } = useAuth();

  const { data, pageNumber, setPageNumber, isLoading, error } =
    usePagedQuery<PersonDto>(
      (page, size) => getCitizensPage(page, size, token!),
      PAGE_SIZE,
      [token],
      { enabled: !!token, errorMessage: "Failed to load citizens." }
    );

  const citizens = data?.items ?? [];
  const total = data?.totalCount ?? 0;
  const totalPages = data?.totalPages ?? 1;
  const pageSize = data?.pageSize ?? PAGE_SIZE;

  const currentPage = data?.pageNumber ?? pageNumber;
  const range = getPageRange(currentPage, pageSize, total);

  const [selectedCitizenId, setSelectedCitizenId] = useState<string | null>(
    null
  );

  const selectedCitizen =
    selectedCitizenId != null
      ? citizens.find((c) => c.id === selectedCitizenId) ?? null
      : null;

  const openEditor = (id: string) => setSelectedCitizenId(id);
  const closeEditor = () => setSelectedCitizenId(null);

  const paginationDisabled = isLoading || !token;

  return (
    <div className="citizens-page">
      <h1 className="page-title">Citizens</h1>

      <div className="citizens-page-toolbar">
        <div className="citizens-page-toolbar-left">
          {isLoading && <span className="card-sub">Loading citizens...</span>}
          {error && <p className="error-text">{error}</p>}

          {!isLoading && !error && total > 0 && (
            <span className="card-sub">
              Showing {range.start}-{range.end} of {total} citizens
            </span>
          )}
        </div>

        <Pagination
          page={pageNumber}
          totalPages={Math.max(1, totalPages)}
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
            totalPages={Math.max(1, totalPages)}
            onChange={setPageNumber}
            disabled={paginationDisabled}
          />
        </>
      )}

      {!isLoading && !error && total === 0 && (
        <p className="card-sub">No citizens yet.</p>
      )}

      <CitizenDetailsModal
        person={selectedCitizen}
        isOpen={selectedCitizen != null}
        onClose={closeEditor}
        onPersonUpdated={() => {}}
      />
    </div>
  );
};

export default CitizensPage;
