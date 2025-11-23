import "../../../styles/population/pagination.css";

interface PaginationProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}

const Pagination = ({ page, totalPages, onChange }: PaginationProps) => {
  const canGoPrev = page > 1;
  const canGoNext = page < totalPages;

  return (
    <div className="pagination">
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

      <span>
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

export default Pagination;
