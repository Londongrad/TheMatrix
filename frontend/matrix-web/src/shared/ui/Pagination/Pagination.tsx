// src/shared/components/Pagination.tsx
import Button from "@shared/ui/controls/Button/Button";
import "./pagination.css";

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
      <Button size="sm" disabled={isPrevDisabled} onClick={() => safeChange(1)}>
        First
      </Button>

      <Button
        size="sm"
        disabled={isPrevDisabled}
        onClick={() => safeChange(page - 1)}
      >
        Previous
      </Button>

      <span>
        Page {page} of {totalPages}
      </span>

      <Button
        size="sm"
        disabled={isNextDisabled}
        onClick={() => safeChange(page + 1)}
      >
        Next
      </Button>

      <Button
        size="sm"
        disabled={isNextDisabled}
        onClick={() => safeChange(totalPages)}
      >
        Last
      </Button>
    </div>
  );
};

export default Pagination;
