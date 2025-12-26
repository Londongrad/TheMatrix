import "@services/population/styles/pagination.css";

interface PaginationProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
  disabled?: boolean;
}

const Pagination = ({
  page,
  totalPages,
  onChange,
  disabled = false,
}: PaginationProps) => {
  const canGoPrev = page > 1;
  const canGoNext = page < totalPages;

  const isPrevDisabled = disabled || !canGoPrev;
  const isNextDisabled = disabled || !canGoNext;

  const safeChange = (nextPage: number) => {
    if (disabled) return;
    if (nextPage === page) return;
    onChange(nextPage);
  };

  return (
    <div className="pagination" aria-disabled={disabled}>
      <button
        className="btn btn-sm"
        disabled={isPrevDisabled}
        onClick={() => safeChange(1)}
      >
        First
      </button>

      <button
        className="btn btn-sm"
        disabled={isPrevDisabled}
        onClick={() => safeChange(page - 1)}
      >
        Previous
      </button>

      <span>
        Page {page} of {totalPages}
      </span>

      <button
        className="btn btn-sm"
        disabled={isNextDisabled}
        onClick={() => safeChange(page + 1)}
      >
        Next
      </button>

      <button
        className="btn btn-sm"
        disabled={isNextDisabled}
        onClick={() => safeChange(totalPages)}
      >
        Last
      </button>
    </div>
  );
};

export default Pagination;
